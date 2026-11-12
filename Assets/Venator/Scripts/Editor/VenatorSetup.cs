using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

/// <summary>
/// One-click setup for VENATOR Weeks 00-02.
/// Menu: Venator > Build Week 02 Test Scene
/// Creates materials, animator controllers, the Player / Light Bolt / Grim prefabs,
/// and the District 1 (Harbor Gate) greybox scene, then adds it to Build Settings.
/// Safe to run again: it overwrites what it made before.
/// </summary>
public static class VenatorSetup
{
    const string Root = "Assets/Venator";
    const string ArtDir = Root + "/Art";
    const string PrefabDir = Root + "/Prefabs";
    const string MatDir = Root + "/Materials";
    const string AnimDir = Root + "/Animation";
    const string SceneDir = Root + "/Scenes";
    const string ArmsFbx = ArtDir + "/Venator_Arms.fbx";
    const string GrimFbx = ArtDir + "/Grim.fbx";
    const string ScenePath = SceneDir + "/District01_Greybox.unity";
    // Blender models (built facing Blender -Y) import into Unity facing -Z, so turn them around.
    static readonly Quaternion ModelFix = Quaternion.Euler(0f, 180f, 0f);

    // ------------------------------------------------------------------ menu items
    [MenuItem("Venator/Build Week 02 Test Scene", priority = 1)]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var d in new[] { ArtDir, PrefabDir, MatDir, AnimDir, SceneDir }) EnsureFolder(d);

        ConfigureArt();
        var armsCtrl = BuildController(ArmsFbx, AnimDir + "/VenatorArms.controller",
            new[] { "Fire", "Reload", "Lash" }, new string[0]);
        var grimCtrl = BuildController(GrimFbx, AnimDir + "/Grim.controller",
            new[] { "Telegraph", "Attack", "Stagger", "Banish" }, new[] { "Telegraph", "Banish" });

        var bolt = BuildBoltPrefab();
        var player = BuildPlayerPrefab(bolt, armsCtrl);
        var grim = BuildGrimPrefab(grimCtrl);
        BuildScene(player, grim);

        Debug.Log("<b>VENATOR:</b> Week 02 test scene built at " + ScenePath + ". Press Play.");
    }

    [MenuItem("Venator/Reconfigure Imported Art Only", priority = 20)]
    public static void ConfigureArt()
    {
        ConfigureModel(ArmsFbx, "Idle", "Walk");
        ConfigureModel(GrimFbx, "Idle", "Walk");
    }

    // ------------------------------------------------------------------ art import
    static void ConfigureModel(string path, params string[] loopingClips)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) { Debug.LogWarning($"VENATOR: {path} not found - using placeholder shapes instead."); return; }

        importer.animationType = ModelImporterAnimationType.Generic;
        importer.bakeAxisConversion = true;   // Blender is Z-up; bake the fix into the mesh
        importer.importAnimation = true;
        importer.SaveAndReimport();

        var clips = importer.defaultClipAnimations;
        foreach (var c in clips)
        {
            bool loop = loopingClips.Any(l => ShortName(c.name) == l);
            c.loopTime = loop;
            c.loopPose = loop;
            c.name = ShortName(c.name); // "GrimRig|Idle" -> "Idle"
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    static string ShortName(string clipName)
    {
        int bar = clipName.LastIndexOf('|');
        return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
    }

    static AnimationClip FindClip(string fbx, string name) =>
        AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__") && ShortName(c.name) == name);

    // ------------------------------------------------------------------ animator controllers
    static AnimatorController BuildController(string fbx, string path, string[] triggers, string[] noReturn)
    {
        var idleClip = FindClip(fbx, "Idle");
        if (idleClip == null) return null;

        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        foreach (var t in triggers) ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle");
        idle.motion = idleClip;
        sm.defaultState = idle;

        var walkClip = FindClip(fbx, "Walk");
        if (walkClip != null)
        {
            var walk = sm.AddState("Walk");
            walk.motion = walkClip;
            var toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false; toWalk.duration = 0.15f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.3f, "Speed");
            var toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false; toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.3f, "Speed");
        }

        foreach (var t in triggers)
        {
            var clip = FindClip(fbx, t);
            if (clip == null) continue;
            var state = sm.AddState(t);
            state.motion = clip;
            var enter = sm.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, t);
            enter.hasExitTime = false;
            enter.duration = 0.05f;
            enter.canTransitionToSelf = true;
            if (!noReturn.Contains(t))
            {
                var back = state.AddTransition(idle);
                back.hasExitTime = true;
                back.exitTime = 0.95f;
                back.duration = 0.1f;
            }
        }
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // ------------------------------------------------------------------ materials
    static Material Mat(string name, Color color, float emission = 0f, string shaderName = "Universal Render Pipeline/Lit")
    {
        string path = $"{MatDir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ prefabs
    static GameObject InstantiateModel(string path, Transform parent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }

    static void RemoveCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    static T SavePrefab<T>(GameObject root, string name) where T : Component
    {
        string path = $"{PrefabDir}/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<T>();
    }

    static Projectile BuildBoltPrefab()
    {
        var sun = Mat("M_SunBolt", new Color(1f, 0.85f, 0.45f), 4f);
        var trailMat = Mat("M_BoltTrail", new Color(1f, 0.8f, 0.4f, 0.6f), 0f, "Universal Render Pipeline/Particles/Unlit");

        var root = new GameObject("LightBolt");
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        var col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true; col.direction = 2; col.radius = 0.05f; col.height = 0.5f;
        root.AddComponent<Projectile>();

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "BoltMesh";
        RemoveCollider(visual);
        visual.transform.SetParent(root.transform, false);
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(0.05f, 0.22f, 0.05f);
        var mr = visual.GetComponent<MeshRenderer>();
        mr.sharedMaterial = sun; mr.shadowCastingMode = ShadowCastingMode.Off;

        var trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.12f; trail.widthMultiplier = 0.06f; trail.sharedMaterial = trailMat;
        trail.shadowCastingMode = ShadowCastingMode.Off;

        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.type = LightType.Point; light.range = 3f; light.intensity = 2f; light.color = new Color(1f, 0.85f, 0.45f);
        light.shadows = LightShadows.None;

        return SavePrefab<Projectile>(root, "LightBolt");
    }

    static GameObject BuildPlayerPrefab(Projectile bolt, RuntimeAnimatorController armsCtrl)
    {
        var root = new GameObject("Player");
        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.4f; cc.slopeLimit = 50f; cc.skinWidth = 0.04f;
        root.AddComponent<VenatorInput>();

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.03f; cam.fieldOfView = 75f;
        camGo.AddComponent<AudioListener>();

        Animator armsAnim = null;
        var arms = InstantiateModel(ArmsFbx, camGo.transform);
        if (arms != null)
        {
            arms.name = "Viewmodel Arms";
            armsAnim = arms.GetComponent<Animator>();
            if (armsAnim == null) armsAnim = arms.AddComponent<Animator>();
            armsAnim.runtimeAnimatorController = armsCtrl;
            armsAnim.applyRootMotion = false;
            foreach (var r in arms.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }
        else
        {   // placeholder crossbow block if the FBX is missing
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Placeholder Crossbow";
            RemoveCollider(block);
            block.transform.SetParent(camGo.transform, false);
            block.transform.localPosition = new Vector3(0.15f, -0.18f, 0.55f);
            block.transform.localScale = new Vector3(0.06f, 0.06f, 0.45f);
            block.GetComponent<Renderer>().sharedMaterial = Mat("M_Wood", new Color(0.33f, 0.2f, 0.11f));
        }

        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(camGo.transform, false);
        muzzle.localPosition = new Vector3(0.15f, -0.17f, 0.78f);
        if (arms != null) FitViewmodel(arms, camGo.transform, muzzle);

        var mover = root.AddComponent<FirstPersonController>();
        mover.cameraPivot = camGo.transform;
        var look = root.AddComponent<MouseLook>();
        look.cameraPivot = camGo.transform;
        root.AddComponent<HealthSystem>();
        var whip = root.AddComponent<WhipWeapon>();
        whip.cam = camGo.transform;
        var weapons = root.AddComponent<WeaponController>();
        weapons.cam = cam; weapons.muzzle = muzzle; weapons.boltPrefab = bolt; weapons.viewmodelAnimator = armsAnim;
        root.AddComponent<DebugHUD>();
        root.AddComponent<AudioSource>();

        return SavePrefab<Transform>(root, "Player").gameObject;
    }

    /// <summary>
    /// Puts the viewmodel arms in front of the camera no matter how the FBX import turned out.
    /// Measures where three bones landed, tries all 24 axis-aligned rotations and three unit scales,
    /// and keeps the one that puts the hands and crossbow closest to where they belong on screen.
    /// </summary>
    static void FitViewmodel(GameObject arms, Transform cam, Transform muzzle)
    {
        foreach (var smr in arms.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true; // never cull the arms because of stale bounds

        var all = arms.GetComponentsInChildren<Transform>(true);
        string[] names = { "Hand.R", "Hand.L", "Crossbow", "Root" };
        Vector3[] targets =
        {
            new Vector3(0.15f, -0.22f, 0.42f), new Vector3(-0.06f, -0.20f, 0.56f),
            new Vector3(0.15f, -0.18f, 0.42f), new Vector3(0f, -0.5f, 0f)
        };
        var bones = names.Select(n => all.FirstOrDefault(t => t.name == n)).ToArray();
        if (bones.Any(b => b == null))
        {
            arms.transform.localRotation = ModelFix;
            Debug.LogWarning("VENATOR: arm bones not found, so the arms were just turned 180 degrees. Child names: " +
                string.Join(", ", all.Take(15).Select(t => t.name)));
            return;
        }

        arms.transform.localPosition = Vector3.zero;
        arms.transform.localRotation = Quaternion.identity;
        arms.transform.localScale = Vector3.one;
        Vector3[] pts = bones.Select(b => cam.InverseTransformPoint(b.position)).ToArray();

        var rotations = new List<Quaternion>();
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                for (int z = 0; z < 4; z++)
                {
                    var q = Quaternion.Euler(x * 90f, y * 90f, z * 90f);
                    if (!rotations.Any(r => Quaternion.Angle(r, q) < 1f)) rotations.Add(q);
                }

        float bestErr = float.MaxValue, bestScale = 1f;
        Quaternion bestRot = Quaternion.identity;
        foreach (float sc in new[] { 1f, 0.01f, 100f })
            foreach (var q in rotations)
            {
                float err = 0f;
                for (int i = 0; i < pts.Length; i++) err += (q * (pts[i] * sc) - targets[i]).sqrMagnitude;
                if (err < bestErr) { bestErr = err; bestRot = q; bestScale = sc; }
            }

        arms.transform.localRotation = bestRot;
        arms.transform.localScale = Vector3.one * bestScale;
        muzzle.localPosition = bestRot * (pts[2] * bestScale) + new Vector3(0f, 0.03f, 0.33f);
        Debug.Log($"VENATOR: viewmodel fitted - rotation {bestRot.eulerAngles}, scale {bestScale}, error {Mathf.Sqrt(bestErr):0.000} m");
        if (bestErr > 0.05f) Debug.LogWarning("VENATOR: viewmodel fit is rough - check Player/Main Camera/Viewmodel Arms in the prefab.");
    }

    static GameObject BuildGrimPrefab(RuntimeAnimatorController grimCtrl)
    {
        var root = new GameObject("Grim");
        var col = root.AddComponent<CapsuleCollider>();
        col.height = 1.7f; col.radius = 0.45f; col.center = new Vector3(0f, 0.85f, 0f);
        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f; agent.height = 1.7f; agent.stoppingDistance = 1.6f;
        agent.angularSpeed = 360f; agent.acceleration = 12f;
        agent.enabled = false; // EnemyAI turns it on after the NavMesh is built

        var model = InstantiateModel(GrimFbx, root.transform);
        if (model != null)
        {
            model.name = "Grim Model";
            model.transform.localRotation = ModelFix; // face the same way the NavMeshAgent moves
            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = grimCtrl;
            anim.applyRootMotion = false;
        }
        else
        {   // placeholder body + glowing eyes
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Placeholder Body"; RemoveCollider(body);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            body.transform.localScale = new Vector3(0.9f, 0.85f, 0.9f);
            body.GetComponent<Renderer>().sharedMaterial = Mat("M_GrimBody", new Color(0.3f, 0.33f, 0.43f));
            foreach (float x in new[] { -0.15f, 0.15f })
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye"; RemoveCollider(eye);
                eye.transform.SetParent(root.transform, false);
                eye.transform.localPosition = new Vector3(x, 1.35f, 0.38f);
                eye.transform.localScale = Vector3.one * 0.15f;
                eye.GetComponent<Renderer>().sharedMaterial = Mat("M_GrimEye", new Color(1f, 0.72f, 0.15f), 4f);
            }
        }

        var tl = new GameObject("TelegraphLight").AddComponent<Light>();
        tl.transform.SetParent(root.transform, false);
        tl.transform.localPosition = new Vector3(0f, 1.4f, 0.5f);
        tl.type = LightType.Point; tl.range = 3.5f; tl.intensity = 0f; tl.color = new Color(1f, 0.15f, 0.1f);
        tl.shadows = LightShadows.None;

        root.AddComponent<EnemyHealth>();
        var ai = root.AddComponent<EnemyAI>();
        ai.telegraphLight = tl;

        return SavePrefab<Transform>(root, "Grim").gameObject;
    }

    // ------------------------------------------------------------------ scene
    static Transform greybox;

    static GameObject Block(string name, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(greybox, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static void Lamp(Vector3 pos, Material post, Material flame)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = "Wardlight (placeholder)";
        p.transform.SetParent(greybox, false);
        p.transform.localPosition = pos + Vector3.up * 1.6f;
        p.transform.localScale = new Vector3(0.15f, 1.6f, 0.15f);
        p.GetComponent<Renderer>().sharedMaterial = post;

        var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        f.name = "Flame"; RemoveCollider(f);
        f.transform.SetParent(greybox, false);
        f.transform.localPosition = pos + Vector3.up * 3.35f;
        f.transform.localScale = Vector3.one * 0.35f;
        f.GetComponent<Renderer>().sharedMaterial = flame;

        var l = new GameObject("Lamp Light").AddComponent<Light>();
        l.transform.SetParent(f.transform, false);
        l.type = LightType.Point; l.range = 12f; l.intensity = 4f; l.color = new Color(1f, 0.7f, 0.35f);
        l.shadows = LightShadows.Soft;
    }

    static void BuildScene(GameObject playerPrefab, GameObject grimPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.GetComponent<Camera>() != null) Object.DestroyImmediate(go);           // player brings its own camera
            else if (go.TryGetComponent(out Light sunLight))
            {
                go.name = "Moonlight";
                go.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                sunLight.color = new Color(0.62f, 0.7f, 0.95f);
                sunLight.intensity = 0.6f;
            }
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.12f, 0.14f, 0.22f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.ambientIntensity = 0.6f;

        var gbRoot = new GameObject("Greybox (District 1 - Harbor Gate)");
        gbRoot.AddComponent<NavMeshSurface>();
        gbRoot.AddComponent<NavMeshBaker>();
        greybox = gbRoot.transform;

        var ground = Mat("M_Grey_Street", new Color(0.30f, 0.31f, 0.34f));
        var wall = Mat("M_Grey_Building", new Color(0.20f, 0.22f, 0.29f));
        var crate = Mat("M_Grey_Crate", new Color(0.42f, 0.30f, 0.18f));
        var pier = Mat("M_Grey_Pier", new Color(0.35f, 0.27f, 0.20f));
        var gold = Mat("M_Grey_SlideBeam", new Color(0.79f, 0.56f, 0.12f), 0.6f);
        var post = Mat("M_Grey_LampPost", new Color(0.08f, 0.09f, 0.12f));
        var flame = Mat("M_Grey_LampFlame", new Color(1f, 0.75f, 0.35f), 5f);

        // street and outer walls (z = -10 to 110, x = -20 to 20)
        Block("Street", new Vector3(0f, -0.5f, 50f), new Vector3(40f, 1f, 120f), ground);
        Block("South Wall", new Vector3(0f, 4f, -10.5f), new Vector3(40f, 8f, 1f), wall);
        Block("North Wall (Harbor)", new Vector3(0f, 4f, 110.5f), new Vector3(40f, 8f, 1f), wall);
        var rng = new System.Random(7);
        for (float z = -4f; z < 110f; z += 12f)
        {
            float hl = rng.Next(6, 15), hr = rng.Next(6, 15);
            Block("Building L", new Vector3(-17f, hl / 2f, z), new Vector3(6f, hl, 11.5f), wall);
            Block("Building R", new Vector3(17f, hr / 2f, z), new Vector3(6f, hr, 11.5f), wall);
        }

        // Section A (z 0-35): jump practice - crates and a two-step stack
        Block("Crate", new Vector3(-3f, 0.5f, 12f), new Vector3(2f, 1f, 2f), crate);
        Block("Crate", new Vector3(3f, 0.5f, 16f), new Vector3(2f, 1f, 2f), crate);
        Block("Crate Step 1m", new Vector3(8f, 0.5f, 22f), new Vector3(2f, 1f, 2f), crate);
        Block("Crate Step 2m", new Vector3(8f, 1f, 24f), new Vector3(2f, 2f, 2f), crate);
        Block("Crate Ledge", new Vector3(10.5f, 1f, 26f), new Vector3(3f, 2f, 6f), crate);

        // Section B (z 40): slide gate - gold beam at 1.2 m, walls either side force a slide
        Block("Slide Beam (gold = slide under)", new Vector3(0f, 1.6f, 40f), new Vector3(12f, 0.8f, 1f), gold);
        Block("Gate Wall L", new Vector3(-10f, 3f, 40f), new Vector3(8f, 6f, 1f), wall);
        Block("Gate Wall R", new Vector3(10f, 3f, 40f), new Vector3(8f, 6f, 1f), wall);
        Block("Gate Lintel", new Vector3(0f, 4f, 40f), new Vector3(12f, 4f, 1f), wall);

        // Section C (z 54-80): ramp up to the raised pier
        float rise = 1.5f, run = 6f;
        float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
        Block("Pier Ramp", new Vector3(0f, rise / 2f - 0.2f, 57f), new Vector3(8f, 0.4f, Mathf.Sqrt(rise * rise + run * run)), pier, new Vector3(-angle, 0f, 0f));
        Block("Pier", new Vector3(0f, rise / 2f, 70f), new Vector3(16f, rise, 20f), pier);
        Block("Pier Crate", new Vector3(-5f, rise + 0.5f, 74f), new Vector3(2f, 1f, 2f), crate);

        // Section D (z 85-105): small arena with cover
        Block("Cover", new Vector3(-6f, 0.75f, 90f), new Vector3(2f, 1.5f, 2f), crate);
        Block("Cover", new Vector3(5f, 0.75f, 95f), new Vector3(2f, 1.5f, 2f), crate);
        Block("Cover", new Vector3(0f, 0.75f, 101f), new Vector3(4f, 1.5f, 1f), crate);

        Lamp(new Vector3(-9f, 0f, 4f), post, flame);
        Lamp(new Vector3(9f, 0f, 33f), post, flame);
        Lamp(new Vector3(-7f, rise, 64f), post, flame);
        Lamp(new Vector3(9f, 0f, 92f), post, flame);

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -5f), Quaternion.identity);

        var spawns = new List<Vector3>
        {
            new Vector3(-4f, 0f, 26f), new Vector3(4f, 0f, 31f),             // before the slide gate
            new Vector3(-3f, rise, 68f), new Vector3(3f, rise, 75f),         // on the pier
            new Vector3(-5f, 0f, 96f), new Vector3(6f, 0f, 102f)             // arena
        };
        var grims = new GameObject("Grims").transform;
        foreach (var p in spawns)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(grimPrefab, grims);
            g.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 180f, 0f));
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Selection.activeGameObject = player;
    }

    // ------------------------------------------------------------------ util
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}

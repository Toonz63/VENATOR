"""
VENATOR - placeholder art generator (Week 01 + Week 02 art tasks)
Phoenix Toon / Team Toonz

Builds two low-poly, rigged, animated models and exports them as FBX for Unity:
  * Venator_Arms.fbx  - first-person viewmodel arms holding the repeater crossbow
                         Actions: Idle, Walk, Fire, Reload, Lash
  * Grim.fbx          - the basic Grim enemy
                         Actions: Idle, Walk, Telegraph, Attack, Stagger, Banish

How to run (Blender 4.2+ or 5.x):
  1. Open Blender, go to the Scripting tab, click Open, pick this file.
  2. Change OUT_DIR below if you want the FBX files somewhere else.
  3. Press Run Script (Alt+P). The .fbx and .blend files appear in OUT_DIR.
Or from a terminal:  blender --background --python venator_art.py

Axis notes: everything faces Blender -Y. It imports into Unity facing -Z, so VenatorSetup turns the models 180 degrees.
The arms are modeled around a camera sitting at the origin looking down -Y.
"""
import bpy, math, os
from mathutils import Vector

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.getcwd(), "export")
os.makedirs(OUT_DIR, exist_ok=True)


# ------------------------------------------------------------------ helpers
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    scn.render.fps = 30
    return scn


def material(name, rgb, emit=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    if hasattr(m, "use_nodes") and not m.use_nodes:
        m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Roughness"].default_value = 0.7
    if emit > 0:
        bsdf.inputs["Emission Color"].default_value = (*rgb, 1)
        bsdf.inputs["Emission Strength"].default_value = emit
    return m


def part(kind, name, loc, scale, mat, bone, rot=(0, 0, 0), **kw):
    """Create a primitive, give it one material and a vertex group fully weighted to `bone`."""
    if kind == "cube":
        bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    elif kind == "cyl":
        bpy.ops.mesh.primitive_cylinder_add(vertices=kw.get("verts", 10), radius=0.5, depth=1, location=loc, rotation=rot)
    elif kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=kw.get("seg", 12), ring_count=kw.get("rings", 8), radius=0.5, location=loc, rotation=rot)
    elif kind == "cone":
        bpy.ops.mesh.primitive_cone_add(vertices=kw.get("verts", 8), radius1=0.5, radius2=0.0, depth=1, location=loc, rotation=rot)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.data.materials.append(mat)
    vg = ob.vertex_groups.new(name=bone)
    vg.add(list(range(len(ob.data.vertices))), 1.0, "REPLACE")
    return ob


def limb(a, b, r, mat, bone, name, kind="cyl"):
    """Cylinder (or cone pointing from a to b) between two points."""
    a, b = Vector(a), Vector(b)
    d = b - a
    rot = d.to_track_quat("Z", "Y").to_euler()
    return part(kind, name, (a + b) / 2, (r, r, d.length), mat, bone, rot=rot, verts=10)


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.shade_flat()
    return ob


def armature(name, bones):
    """bones: list of (name, head, tail, parent)"""
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm = bpy.context.active_object
    arm.name = name
    arm.data.name = name + "_Data"
    eb = arm.data.edit_bones
    eb.remove(eb[0])
    for bname, head, tail, parent in bones:
        b = eb.new(bname)
        b.head, b.tail = Vector(head), Vector(tail)
        if parent:
            b.parent = eb[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def bind(mesh, arm):
    mesh.parent = arm
    mod = mesh.modifiers.new("Armature", "ARMATURE")
    mod.object = arm


def new_action(arm, name):
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = act
    # slotted actions (Blender 4.4+) pick a slot automatically on first key
    return act


def key(arm, frame, poses):
    """poses: {bone: {"loc": (x,y,z), "rot": (deg x,y,z), "scale": s}} - unspecified channels reset."""
    for pb in arm.pose.bones:
        p = poses.get(pb.name, {})
        pb.rotation_mode = "XYZ"
        pb.location = p.get("loc", (0, 0, 0))
        pb.rotation_euler = [math.radians(a) for a in p.get("rot", (0, 0, 0))]
        s = p.get("scale", 1.0)
        pb.scale = (s, s, s) if isinstance(s, (int, float)) else s
        pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("rotation_euler", frame=frame)
        pb.keyframe_insert("scale", frame=frame)


def make_actions(arm, actions):
    """actions: {name: [(frame, poses), ...]}. Each becomes its own Action / FBX take."""
    made = []
    for name, keys in actions.items():
        act = new_action(arm, name)
        for frame, poses in keys:
            key(arm, frame, poses)
        made.append(act)
    # leave the first action active; the FBX exporter writes every action as its own take
    arm.animation_data.action = made[0]
    return made


def export(path, objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        mesh_smooth_type="FACE", path_mode="AUTO")


# ------------------------------------------------------------------ colors
COAT = (0.10, 0.11, 0.16)
GLOVE = (0.22, 0.14, 0.09)
WOOD = (0.33, 0.20, 0.11)
BRASS = (0.79, 0.56, 0.12)
IRON = (0.35, 0.37, 0.42)
STRING = (0.85, 0.82, 0.72)
SUN = (1.0, 0.85, 0.45)
GRIM = (0.30, 0.33, 0.43)
GRIM_DARK = (0.16, 0.18, 0.25)
EYE = (1.0, 0.72, 0.15)


# ================================================================== ARMS
def build_arms():
    reset_scene()
    m_coat, m_glove, m_wood = material("M_Coat", COAT), material("M_Glove", GLOVE), material("M_Wood", WOOD)
    m_brass, m_iron, m_string, m_sun = material("M_Brass", BRASS), material("M_Iron", IRON), material("M_String", STRING), material("M_SunBolt", SUN, 3.0)

    # Camera = origin looking -Y. Viewer's right = Blender -X (becomes Unity +X).
    # Right arm (holds crossbow grip) and left arm (supports fore-end, swings whip).
    R0, R1, R2 = (-0.30, 0.05, -0.38), (-0.20, -0.25, -0.28), (-0.15, -0.42, -0.22)
    L0, L1, L2 = (0.30, 0.05, -0.38), (0.16, -0.30, -0.26), (0.06, -0.56, -0.20)
    bones = [
        ("Root", (0, 0, -0.5), (0, 0, -0.4), None),
        ("UpperArm.R", R0, R1, "Root"), ("Forearm.R", R1, R2, "UpperArm.R"), ("Hand.R", R2, (-0.15, -0.50, -0.20), "Forearm.R"),
        ("Crossbow", (-0.15, -0.42, -0.18), (-0.15, -0.70, -0.18), "Hand.R"),
        ("UpperArm.L", L0, L1, "Root"), ("Forearm.L", L1, L2, "UpperArm.L"), ("Hand.L", L2, (0.04, -0.64, -0.20), "Forearm.L"),
    ]
    arm = armature("ArmsRig", bones)

    p = [
        limb(R0, R1, 0.12, m_coat, "UpperArm.R", "SleeveR"), limb(R1, R2, 0.095, m_coat, "Forearm.R", "ForearmR"),
        part("cube", "HandR", (-0.15, -0.45, -0.22), (0.08, 0.10, 0.07), m_glove, "Hand.R"),
        limb(L0, L1, 0.12, m_coat, "UpperArm.L", "SleeveL"), limb(L1, L2, 0.095, m_coat, "Forearm.L", "ForearmL"),
        part("cube", "HandL", (0.05, -0.59, -0.20), (0.08, 0.10, 0.07), m_glove, "Hand.L"),
        # repeater crossbow: stock, magazine, limbs, string, bolt
        part("cube", "Stock", (-0.15, -0.55, -0.17), (0.05, 0.42, 0.05), m_wood, "Crossbow"),
        part("cube", "Magazine", (-0.15, -0.55, -0.125), (0.045, 0.14, 0.045), m_brass, "Crossbow"),
        part("cube", "LimbL", (-0.06, -0.74, -0.17), (0.20, 0.025, 0.025), m_iron, "Crossbow", rot=(0, 0, math.radians(-15))),
        part("cube", "LimbR", (-0.24, -0.74, -0.17), (0.20, 0.025, 0.025), m_iron, "Crossbow", rot=(0, 0, math.radians(15))),
        part("cube", "String", (-0.15, -0.70, -0.17), (0.34, 0.006, 0.006), m_string, "Crossbow"),
        part("cyl", "Bolt", (-0.15, -0.72, -0.145), (0.012, 0.012, 0.24), m_sun, "Crossbow", rot=(math.radians(90), 0, 0)),
        # coiled whip on the left wrist
        part("cyl", "WhipCoil", (0.10, -0.42, -0.24), (0.07, 0.07, 0.04), m_iron, "Forearm.L", rot=(0, math.radians(90), 0), verts=12),
    ]
    mesh = join(p, "Venator_Arms")
    bind(mesh, arm)

    def sway(z, x=0):  # helper for small idle offsets on the root
        return {"Root": {"loc": (x, z, 0)}}

    actions = {
        "Idle": [(1, sway(0)), (30, {"Root": {"loc": (0.003, 0.008, 0), "rot": (0.6, 0, 0.4)}}), (60, sway(0))],
        "Walk": [(1, sway(0)), (8, {"Root": {"loc": (0.012, -0.018, 0), "rot": (0, 0, 1.2)}}), (15, sway(0)),
                 (23, {"Root": {"loc": (-0.012, -0.018, 0), "rot": (0, 0, -1.2)}}), (30, sway(0))],
        "Fire": [(1, {}), (3, {"Root": {"loc": (0, 0.015, -0.05)}, "Crossbow": {"rot": (9, 0, 0)}}),
                 (12, {})],
        "Reload": [(1, {}), (8, {"Root": {"loc": (0, -0.06, -0.02), "rot": (-6, 0, -12)}, "Crossbow": {"rot": (0, 25, 0)}}),
                   (16, {"Root": {"loc": (0, -0.06, -0.02), "rot": (-6, 0, -12)}, "Crossbow": {"rot": (0, 25, 0)},
                         "UpperArm.L": {"rot": (-20, 0, 15)}, "Forearm.L": {"rot": (-25, 0, 0)}}),
                   (26, {"Root": {"loc": (0, -0.06, -0.02), "rot": (-6, 0, -12)}, "Crossbow": {"rot": (0, 25, 0)},
                         "UpperArm.L": {"rot": (10, 0, -5)}, "Forearm.L": {"rot": (5, 0, 0)}}),
                   (34, {"Root": {"loc": (0, -0.03, -0.01), "rot": (-3, 0, -6)}, "Crossbow": {"rot": (0, 10, 0)}}),
                   (42, {})],
        "Lash": [(1, {}), (5, {"UpperArm.L": {"rot": (35, 0, -20)}, "Forearm.L": {"rot": (40, 0, 0)}}),
                 (9, {"UpperArm.L": {"rot": (-30, 0, 35)}, "Forearm.L": {"rot": (-30, 0, 0)}, "Root": {"rot": (0, 0, -3)}}),
                 (18, {})],
    }
    make_actions(arm, actions)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Venator_Arms.blend"))
    export(os.path.join(OUT_DIR, "Venator_Arms.fbx"), [arm, mesh])
    return arm, mesh


# ================================================================== GRIM
def build_grim():
    reset_scene()
    m_body, m_dark, m_eye = material("M_GrimBody", GRIM), material("M_GrimDark", GRIM_DARK), material("M_GrimEye", EYE, 4.0)
    m_mist = material("M_GrimMist", (0.75, 0.83, 0.9), 0.5)
    bones = [
        ("Root", (0, 0, 0), (0, 0, 0.3), None),
        ("Body", (0, 0, 0.3), (0, 0, 1.1), "Root"),
        ("Head", (0, 0, 1.1), (0, 0, 1.6), "Body"),
        ("Arm.R", (-0.40, 0, 1.05), (-0.62, -0.22, 0.55), "Body"),
        ("Arm.L", (0.40, 0, 1.05), (0.62, -0.22, 0.55), "Body"),
    ]
    arm = armature("GrimRig", bones)
    p = [
        part("sphere", "Torso", (0, 0, 0.85), (0.85, 0.75, 1.05), m_body, "Body", seg=14, rings=10),
        part("cone", "TailRags1", (-0.22, 0.05, 0.25), (0.28, 0.28, 0.5), m_dark, "Root", rot=(math.radians(180), 0, 0)),
        part("cone", "TailRags2", (0.0, 0.10, 0.20), (0.30, 0.30, 0.55), m_dark, "Root", rot=(math.radians(180), 0, 0)),
        part("cone", "TailRags3", (0.22, 0.05, 0.25), (0.28, 0.28, 0.5), m_dark, "Root", rot=(math.radians(180), 0, 0)),
        part("sphere", "HeadBall", (0, -0.05, 1.38), (0.62, 0.55, 0.52), m_body, "Head", seg=12, rings=8),
        part("cone", "HornL", (0.22, 0.0, 1.70), (0.14, 0.14, 0.34), m_dark, "Head", rot=(0, math.radians(-20), 0)),
        part("cone", "HornR", (-0.22, 0.0, 1.70), (0.14, 0.14, 0.34), m_dark, "Head", rot=(0, math.radians(20), 0)),
        part("sphere", "EyeL", (0.13, -0.29, 1.44), (0.12, 0.06, 0.12), m_eye, "Head"),
        part("sphere", "EyeR", (-0.13, -0.29, 1.44), (0.12, 0.06, 0.12), m_eye, "Head"),
        part("cube", "Mouth", (0, -0.30, 1.27), (0.20, 0.03, 0.03), m_dark, "Head"),
        limb((0.36, 0, 1.08), (0.62, -0.22, 0.58), 0.11, m_body, "Arm.L", "ArmL"),
        limb((-0.36, 0, 1.08), (-0.62, -0.22, 0.58), 0.11, m_body, "Arm.R", "ArmR"),
        limb((0.62, -0.22, 0.60), (0.66, -0.36, 0.36), 0.12, m_dark, "Arm.L", "ClawL", kind="cone"),
        limb((-0.62, -0.22, 0.60), (-0.66, -0.36, 0.36), 0.12, m_dark, "Arm.R", "ClawR", kind="cone"),
        part("sphere", "MistPuff", (0, 0.1, 0.12), (0.7, 0.6, 0.18), m_mist, "Root", seg=10, rings=6),
    ]
    mesh = join(p, "Grim")
    bind(mesh, arm)

    actions = {
        "Idle": [(1, {}), (20, {"Body": {"loc": (0, 0.04, 0), "rot": (2, 0, 0)}, "Arm.L": {"rot": (0, -5, 0)}, "Arm.R": {"rot": (0, 5, 0)}}), (40, {})],
        "Walk": [(1, {"Root": {"loc": (0, 0.03, 0)}, "Body": {"rot": (6, 0, 4)}}),
                 (10, {"Root": {"loc": (0, 0.12, 0)}, "Body": {"rot": (6, 0, -4)}, "Arm.L": {"rot": (20, 0, 0)}, "Arm.R": {"rot": (-20, 0, 0)}}),
                 (20, {"Root": {"loc": (0, 0.03, 0)}, "Body": {"rot": (6, 0, 4)}})],
        "Telegraph": [(1, {}), (15, {"Body": {"rot": (-18, 0, 0)}, "Head": {"rot": (-10, 0, 0)},
                                      "Arm.L": {"rot": (-60, 0, 0)}, "Arm.R": {"rot": (-60, 0, 0)}}),
                      (16, {"Body": {"rot": (-18, 0, 0)}, "Head": {"rot": (-10, 0, 0)},
                            "Arm.L": {"rot": (-60, 0, 0)}, "Arm.R": {"rot": (-60, 0, 0)}})],
        "Attack": [(1, {"Body": {"rot": (-18, 0, 0)}, "Head": {"rot": (-10, 0, 0)}, "Arm.L": {"rot": (-60, 0, 0)}, "Arm.R": {"rot": (-60, 0, 0)}}),
                   (5, {"Root": {"loc": (0, 0, 0.35)}, "Body": {"rot": (20, 0, 0)}, "Arm.L": {"rot": (40, 0, 0)}, "Arm.R": {"rot": (40, 0, 0)}}),
                   (18, {})],
        "Stagger": [(1, {}), (4, {"Root": {"loc": (0, 0, -0.25)}, "Body": {"rot": (-25, 0, 8)}, "Head": {"rot": (-15, 10, 0)}}),
                    (20, {})],
        "Banish": [(1, {}), (10, {"Body": {"rot": (-10, 0, 0)}, "Root": {"scale": 1.1}}),
                   (30, {"Root": {"loc": (0, 0.5, 0), "scale": 0.02}, "Body": {"rot": (0, 0, 180)}})],
    }
    make_actions(arm, actions)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Grim.blend"))
    export(os.path.join(OUT_DIR, "Grim.fbx"), [arm, mesh])
    return arm, mesh


if __name__ == "__main__":
    build_arms()
    build_grim()
    print("VENATOR art exported to", OUT_DIR)

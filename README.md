# VENATOR – Weeks 00–02 Step-by-Step Guide
Team Toonz · Phoenix Toon

This folder has everything for Weeks 00–02: all of the code, the Blender art (already exported), concept art, and a test checklist. The two progress reports are separate .docx files.

**Total time:** about 2–3 hours if nothing goes wrong.

---

## Part A — Week 00: Project setup (≈ 30 min)

1. **Install the tools** (skip anything you already have):
   - Unity Hub → Installs → **Unity 6.3 LTS (6000.3.x)**. Windows Build Support is included on Windows.
   - **GitHub Desktop** (it includes Git LFS).
   - **Blender 4.2 or newer**. This is optional; you only need it to open or edit the models.
2. **Create the project:** Unity Hub → New project → template **Universal 3D** → name `VENATOR` → pick a folder → Create. Leave Unity Cloud and Version Control unchecked.
3. **Install packages:** Window → **Package Manager** → *Unity Registry* tab. Install:
   - **Input System** (if it isn't installed already). Click **Yes** if Unity asks to restart.
   - **AI Navigation**
4. **Check the input setting:** Edit → Project Settings → Player → Other Settings → **Active Input Handling** = *Input System Package (New)* or *Both*.
5. **Close Unity.** Copy everything from this folder into your `VENATOR` project folder:
   - `Assets/Venator` → into the project's `Assets` folder
   - `Blender/`, `Docs/`, `.gitignore`, `.gitattributes`, `README.md`, `TESTING.md` → into the project root (next to `Assets`)
   > Windows hides files that start with a dot. In File Explorer, turn on View → Show → Hidden items so you can see them.
6. **Make the GitHub repo:**
   1. GitHub Desktop → File → **Add local repository** → pick the `VENATOR` folder → click **"create a repository"**. Name it `VENATOR` and set Git ignore to **None** (you already have one). Click Create.
   2. Repository → **Open in Command Prompt**, then run `git lfs install` once. Close the window.
   3. Write the commit summary `Week 00: Unity 6.3 URP project, concept art, LFS` → **Commit to main** → **Publish repository**.
   4. Keep it **public**, or add your professor as a collaborator, since you'll be submitting the link.

## Part B — Weeks 01 + 02: Build the playable test scene (≈ 10 min)

7. Open the project in Unity and wait for it to finish compiling. The **Console** (Window → General → Console) should have **no red errors**. If there are any, see Troubleshooting below.
8. Top menu bar → **Venator → Build Week 02 Test Scene**. This one click does the following:
   - configures the imported FBX models (Generic rig, axis fix, looping Idle and Walk)
   - builds the animator controllers (`Animation/VenatorArms.controller`, `Animation/Grim.controller`)
   - builds the prefabs: **Player**, **LightBolt**, **Grim** (in `Prefabs/`)
   - builds and saves the scene `Scenes/District01_Greybox.unity` with the greybox street, slide gate, pier, arena, 4 lamp posts, the player, and 6 Grims
   - adds the scene to the build list
9. Press **Play**, then click inside the Game view to lock the mouse.

| Key | Action |
|---|---|
| WASD / Mouse | Move / look |
| Shift (hold) | Sprint |
| Space | Jump |
| Shift + Ctrl | Slide (needed for the gold beam) |
| Left mouse (hold) | Repeater Crossbow |
| R | Reload |
| Right mouse | Whip lash (interrupts a Grim's red-flash telegraph) |
| H | Drink a Tonic |
| K | Test damage (10) |
| Esc / F1 / F10 | Free cursor / hide help / quit build |

10. **Commit in two steps** so the history matches the reports:
    - Commit `Week 01: player movement, slide, health, District 1 greybox`. Include `Scripts/Player`, `Scripts/Core`, `Scripts/UI`, `Venator_Arms.fbx`, and the scene.
    - Commit `Week 02: crossbow, whip, Grim AI/health, NavMesh, Grim model`. Include everything else.
    - Then click **Push origin**.
    - To commit only some files, untick the rest in GitHub Desktop's left panel, commit, then tick the rest and commit again.

## Part C — Art (already done; read this so you can talk about it)

- `Blender/venator_art.py` builds both models from primitives, rigs them, keyframes every animation, and exports FBX.
  - **Arms:** 8-bone rig; Idle, Walk, Fire, Reload, Lash.
  - **Grim:** 5-bone rig; Idle, Walk, Telegraph, Attack, Stagger, Banish.
- Open `Blender/Venator_Arms.blend` or `Blender/Grim.blend` to look at them. Press Space in the timeline to play an animation. Use the Action dropdown in the Action Editor to switch actions.
- **To change something:** open Blender → **Scripting** tab → Open → `venator_art.py` → edit → **Run Script**.
  1. The new files appear in `Blender/export/`. Copy the `.fbx` files over the ones in `Assets/Venator/Art/`.
  2. In Unity, run **Venator → Reconfigure Imported Art Only**, then **Venator → Build Week 02 Test Scene** again.
- Concept art (the Week 00 art task) is in `Docs/Concept/`.

## Part D — Testing (≈ 20 min)

11. Go through **TESTING.md**, test by test, and write Pass/Fail for each.
    - Anything that fails or behaves oddly goes in the **Problems Encountered** section of the report.
    - Swap in what *actually* happened. The problems I wrote are the ones this code is designed around, but yours count more.

## Part E — Build the executable (≈ 10 min)

12. File → **Build Profiles** → **Windows** (click **Switch Platform** if it isn't active).
    - Check that **Scenes/District01_Greybox** is in the scene list.
    - Click **Build**, make a new folder `Builds/Week02` (it's outside Assets and already git-ignored), and select it.
13. Run `VENATOR.exe` to test it.
14. Right-click the **whole `Week02` folder** → Compress to ZIP. The .exe can't run without the `_Data` folder and DLLs next to it, so the zip needs all of them.

## Part F — Submit (professor said Blackboard or email, by the end of this week)

15. Hand in all of the following:
    - `VENATOR_Progress_Report_01.docx`
    - `VENATOR_Progress_Report_02.docx`
    - the zipped build
    - your GitHub repo link (copy it from GitHub Desktop → Repository → View on GitHub)
16. If the zip is too big for email, put it on Google Drive or OneDrive and send the link, or attach it to a GitHub Release (repo page → Releases → Draft a new release → attach the zip).

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `The type or namespace name 'InputSystem' could not be found` | Install **Input System** (Step 3). |
| `'Unity.AI.Navigation' could not be found` / `NavMeshSurface` missing | Install **AI Navigation** (Step 3). |
| `InvalidOperationException ... UnityEngine.Input` | Set Active Input Handling to *Input System Package (New)* or *Both* (Step 4). |
| No **Venator** menu | There's a compile error. Fix the red Console errors first. |
| Pink / magenta objects | The project isn't URP. Recreate it with the **Universal 3D** template. |
| Mouse look doesn't work | Click inside the Game view. |
| Grims don't move | Check that the Greybox object has **NavMeshSurface** + **NavMeshBaker**. Look for "not on NavMesh" warnings. Re-run the Venator menu item. |
| Arms are missing, sideways, or too close | Select `Art/Venator_Arms.fbx` → Model tab → make sure **Bake Axis Conversion** is ticked → Apply. Or adjust `Player/Main Camera/Viewmodel Arms` position/rotation in the prefab. |
| Bolts come from the wrong spot | Move `Player/Main Camera/Muzzle` to the crossbow tip. |
| Any other error | Copy the full red Console message. It includes the file and line number. |

## Script list (matches the GDD Component List)

| Script | Folder | Job |
|---|---|---|
| VenatorInput | Player | Input System actions defined in code |
| FirstPersonController | Player | Walk, sprint, jump, slide, crouch, teleport |
| MouseLook | Player | Yaw/pitch, cursor lock |
| HealthSystem | Player | Vitality, Tonics, invulnerability, respawn |
| WeaponController | Combat | Crossbow fire/reload/ammo, whip input, arm animations |
| Projectile | Combat | Light Bolt movement + sphere-cast hits |
| WhipWeapon | Combat | Melee box, damage, interrupt |
| EnemyAI | Enemies | Wander → chase → telegraph → attack → recover / stagger |
| EnemyHealth | Enemies | Health, hit flash, Stagger glow, Banish |
| VenatorPhysics | Core | IDamageable + raycasts that ignore the shooter |
| NavMeshBaker | Core | Builds the NavMesh from the greybox at load |
| DebugHUD | UI | Temporary HUD (replaced in Week 03) |
| VenatorSetup | Editor | The one-click "Build Week 02 Test Scene" menu |

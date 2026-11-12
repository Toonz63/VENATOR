# VENATOR – Week 01 & Week 02 Test Checklist

Run these in the **District01_Greybox** scene (press Play, then click the Game view).
Mark each one Pass or Fail. If something fails, write what happened. Real results make better "Problems Encountered" sections.

## Week 01 – Movement, collision, health

| # | Test | How | Expected | Pass/Fail |
|---|------|-----|----------|-----------|
| 1 | Walk | Hold W, A, S, D | Moves at walking speed; can't walk through buildings or crates | Pass |
| 2 | Look | Move the mouse | Camera turns; can't look past straight up/down | Pass |
| 3 | Sprint | Hold Shift + W | Noticeably faster; HUD shows SPRINT | Pass |
| 4 | Jump | Space next to the 1 m crate | Lands on top; can climb the 1 m → 2 m crate stack | Pass |
| 5 | Slide | Sprint at the gold beam, press Ctrl | Slides under it; HUD shows SLIDING | Pass | A little hard because you nead to hold both
| 6 | Crouch under ceiling | Slide so you stop right under the beam | Stays CROUCHED, moves slowly, stands up once clear | Pass |
| 7 | Ramp | Walk up the ramp onto the pier | Smooth, no snagging | Pass |
| 8 | Damage | Press K | Vitality drops 10, red flash | Pass |
| 9 | Tonic | After K, press H | Vitality +40%, Tonics 3 → 2 | Pass |
| 10 | Death/respawn | Press K about 10 times | Defeat message → respawn at start with full Vitality and 3 Tonics | Pass |
| 11 | Cursor | Esc, then click Game view | Cursor frees, then re-locks; the click does **not** fire | Pass |

## Week 02 – Weapons and Grims

| # | Test | How | Expected | Pass/Fail |
|---|------|-----|----------|-----------|
| 12 | Fire | Hold left mouse | Bolts fire about 4–5 per second, BOLTS count drops, arms play Fire | Pass | Cant see my arms or gun
| 13 | Aim | Shoot a crate edge at the crosshair | Bolt hits where the crosshair points | Pass |
| 14 | No tunneling | Shoot a thin wall up close | Bolt stops at the wall | Pass |
| 15 | Reload | Press R after a few shots | RELOADING for about 1.4 s, magazine refills from reserve | Pass |
| 16 | Auto reload / empty | Empty the magazine and reserve | Auto-reloads while reserve lasts; then nothing fires | Pass |
| 17 | Whip | Right-click near a Grim | Grim flashes white and takes damage; 0.55 s cooldown | Pass |
| 18 | Grim sight | Walk toward a Grim | It notices you within about 16 m with line of sight, then chases | Pass |
| 19 | Telegraph | Let a Grim reach you | It stops, red light flashes, rears back for about 0.5 s, then attacks for 15 | Pass |
| 20 | Interrupt | Whip during the red flash | No damage taken; Grim staggers | Pass |
| 21 | Stagger glow | Damage a Grim to below 25% | Turns gold | Pass |
| 22 | Banish | Finish it | Banish animation, disappears, no longer blocks you | Pass |
| 23 | Slide gate | Slide under the beam while chased | Grims can't follow | Pass |
| 24 | Build | Run the Windows .exe | Same results as in the editor; F10 quits | Pass |

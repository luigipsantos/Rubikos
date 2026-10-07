# Rubikos 🧊

A Rubik's cube game and trainer made with Unity 6 while learning.
Turn the cube with the mouse or keyboard, get hints from a near-shortest solver, or practise in training mode.

**Download:** Windows and macOS builds are on the [Releases](../../releases) page.
First launch: the hint engine needs about a minute to prepare (you can play meanwhile). Press **I** in the game for the controls.

<details>
A small thank-you to the team of my very first professional job, who motivated and helped me through my first (very steep) steps. This one is for you. 🧊🎨
</details>

## Roadmap

- [x] Project setup, one cube piece, then all 26 pieces built by a script
- [x] Cube rules in plain C# with unit tests
- [x] Animated turns with a move queue
- [x] Keyboard, mouse (drag a face) and camera controls
- [x] Scramble, undo, reset and UI
- [x] Solver, hints, ghost arrow and training mode
- [x] Sounds, timer, help screen, Windows and macOS builds
- [ ] **Next: cube editor.** Paint the 54 stickers to match a real cube in your hands, then let the game solve it

## How it is built

Rules (`Core`, plain C#, unit tested) are kept apart from visuals (`View`), input (`Input`), the solver (`Solver`) and the interface (`UI`). `Game` connects them.

## Third-party code

`Assets/Scripts/Solver/ThirdParty/Kociemba/` contains a C# port of Herbert Kociemba's two-phase algorithm.

- Algorithm and original Java package: Herbert Kociemba, http://kociemba.org
- C# port: Megalomatt, https://github.com/Megalomatt/Kociemba
- My only change: `K_Tools.cs` (table file handling, see `NOTICE.md`)
- License status: the port has no license file. If you are the author or a rights holder and want it removed, please open an issue and I will remove it promptly.
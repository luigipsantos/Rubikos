# Rubikos

A Rubik's cube game and trainer made with Unity 6 while learning.
Features: mouse and keyboard controls, scramble, undo, and a hint system
that suggests the next move using a Kociemba two-phase solver.

## Third-party code

`Assets/Scripts/Solver/ThirdParty/Kociemba/` contains a C# port of Herbert
Kociemba's two-phase algorithm.

- Algorithm and original Java package: Herbert Kociemba, http://kociemba.org
- C# port: Megalomatt, https://github.com/Megalomatt/Kociemba
- My only change: `K_Tools.cs` (table file handling, see `NOTICE.md`)
- License status: the port has no license file. If you are the author or a rights holder and want it removed, please open an issue and I will remove it promptly.
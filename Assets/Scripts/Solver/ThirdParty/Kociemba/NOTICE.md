# Third-party code: Kociemba two-phase solver (C# port)

Algorithm and original Java package: Herbert Kociemba, http://kociemba.org
(his download page allows free use with a credit line).
C# port for Unity: Megalomatt, https://github.com/Megalomatt/Kociemba (master branch).

Credit line: Two-phase algorithm by Herbert Kociemba (http://kociemba.org),
C# port by Megalomatt.

Changes in this project: K_Tools.cs only. Table files are read and written with
BinaryReader/BinaryWriter into a configurable folder instead of BinaryFormatter
and a hard-coded Windows path (marked "RUBIKOS" inside the file).
All original comments and copyright notices are kept intact.

License status: the C# port has no license file. If you are the author or a rights holder and want this code removed, open an issue and it will be removed promptly.
namespace Rubikos.Core
{
    public readonly struct Sticker
    {
        public readonly Int3 Position; // which cubie it is on
        public readonly Int3 Normal; // which way it faces
        public readonly Face Color; // its color = the face it belongs to when solved

        public Sticker(Int3 position, Int3 normal, Face color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        public Sticker Rotated(Axis axis, int quarterTurns)
            => new Sticker(Position.Rotated(axis, quarterTurns), Normal.Rotated(axis, quarterTurns), Color);
    }
}
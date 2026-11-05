namespace avif;

internal class FourCc
{
    internal uint Value;
    internal string Text;

    internal FourCc(uint value)
    {
        Value = value;
        char[] characters = new char[4];
        characters[0] = (char)(value >> 24);
        characters[1] = (char)((value >> 16) & 255);
        characters[2] = (char)((value >> 8) & 255);
        characters[3] = (char)(value & 255);
        Text = new string(characters);
    }
}

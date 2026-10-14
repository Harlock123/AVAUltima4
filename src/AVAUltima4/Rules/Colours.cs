namespace AVAUltima4.Rules;

public static class Colours
{
    /// <summary>"#rrggbb" to an opaque ARGB colour.</summary>
    public static uint Parse(string hex) => 0xFF000000 | Convert.ToUInt32(hex.TrimStart('#'), 16);
}

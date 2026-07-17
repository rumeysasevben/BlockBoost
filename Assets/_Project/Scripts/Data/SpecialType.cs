public enum SpecialType
{
    None,
    RocketH,        // 4-match yatay → satır temizler
    RocketV,        // 4-match dikey → sütun temizler
    Bomb,           // T/L match → 3x3 patlatır
    ColorBomb       // 5-düz match → swap'lendiği renkten tümünü temizler
}
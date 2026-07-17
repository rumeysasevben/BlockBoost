using System.Collections.Generic;

public class MatchGroup
{
    public List<Fish> fish = new List<Fish>();
    public bool isHorizontal;   // true = yatay, false = dikey

    public int Length => fish.Count;

    /// <summary>
    /// Bu grup için hangi special spawn edilmeli?
    /// </summary>
    public SpecialType GetSpecialType()
    {
        if (Length >= 5) return SpecialType.ColorBomb;   // 5-düz → ColorBomb
        if (Length == 4) return isHorizontal ? SpecialType.RocketH : SpecialType.RocketV;
        return SpecialType.None;
    }

    /// <summary>
    /// Bir balığın grubun parçası olup olmadığını kontrol et.
    /// </summary>
    public bool Contains(Fish f) => fish.Contains(f);

    /// <summary>
    /// Gruptaki ortadaki balık (special burada spawn olabilir, swap noktası belli değilse).
    /// </summary>
    public Fish GetMiddleFish() => fish[fish.Count / 2];
}
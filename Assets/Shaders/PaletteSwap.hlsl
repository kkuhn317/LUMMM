#ifndef PALETTE_SWAP_INCLUDED
#define PALETTE_SWAP_INCLUDED

// Custom Function for Shader Graph (Built-in target, 2022.3).
// Matches InColor against SourcePalette (N x 1) and, on a hit, outputs the color
// at the same index in TargetPalette (N x M rows) at row v = PaletteRow.
// Colors not in the source palette pass through unchanged.
// The color count is NOT hardcoded: it is PaletteSize (= source strip width, up to 64).
//
// Use the _float variant (set the Custom Function node precision to Single).

void PaletteSwap_float(
    float3            InColor,
    UnityTexture2D    SourcePalette,
    UnityTexture2D    TargetPalette,
    UnitySamplerState SS,
    float             PaletteSize,
    float             PaletteRow,
    float             Epsilon,
    out float3        OutColor)
{
    OutColor = InColor;              // default: leave unlisted colors untouched
    int n = (int)PaletteSize;

    [loop]
    for (int idx = 0; idx < 64; idx++)
    {
        if (idx >= n) break;

        float  u      = (idx + 0.5) / PaletteSize;
        // LOD 0 sampling: palettes have no mips, and this avoids derivative
        // issues from sampling inside a loop/branch.
        float3 srcCol = SAMPLE_TEXTURE2D_LOD(SourcePalette.tex, SS.samplerstate, float2(u, 0.5), 0).rgb;

        if (all(abs(InColor - srcCol) <= Epsilon))
        {
            OutColor = SAMPLE_TEXTURE2D_LOD(TargetPalette.tex, SS.samplerstate, float2(u, PaletteRow), 0).rgb;
            break;
        }
    }
}

#endif

#ifndef MASK_PALETTE_SWAP_INCLUDED
#define MASK_PALETTE_SWAP_INCLUDED

// Custom Function (File) for Shader Graph.  Type = File, Name = MaskPaletteSwap, Precision = Single.
//
// DATA-DRIVEN region match. No hardcoded colors. The mask color is matched to the
// nearest entry in RegionIds (an N x 1 strip, left->right = palette columns), and
// the matched region outputs TargetPalette[column, PaletteRow]. Unmatched mask
// pixels (farther than Tolerance from every ID) fall through to InColor (fallback).
// PaletteRow < 0 bypasses everything (resting sprite).
//
// - RegionIds column order == TargetPalette column order (both authored together).
// - Nearest-match keeps close ID pairs (e.g. #FF0000 vs #E5110B) distinct.
// - V is flipped so PaletteRow 0 == the TOP image row of TargetPalette.

void MaskPaletteSwap_float(
    float3 InColor,                 // _MainTex.rgb   (fallback)
    float3 MaskColor,               // _MaskTex.rgb   (region ID)
    UnityTexture2D RegionIds,       // N x 1 strip of ID colors
    float RegionCount,              // N (e.g. 15)
    UnityTexture2D TargetPalette,   // N x Rows
    UnitySamplerState SS,           // Point / Clamp
    float PaletteRows,              // Rows in TargetPalette
    float PaletteRow,               // active row; < 0 = off
    float Tolerance,                // max distance to accept a match (e.g. 0.1)
    out float3 OutColor)
{
    OutColor = InColor; // default: fallback

    if (PaletteRow < 0.0)
        return;

    int   N     = min((int)(RegionCount + 0.5), 64);
    int   best  = -1;
    float bestD = Tolerance * Tolerance; // squared; only accept if within Tolerance

    [loop]
    for (int k = 0; k < N; k++)
    {
        float  uu  = (k + 0.5) / RegionCount;
        float3 idc = SAMPLE_TEXTURE2D_LOD(RegionIds.tex, SS.samplerstate, float2(uu, 0.5), 0).rgb;
        float3 d   = MaskColor - idc;
        float  dd  = dot(d, d);
        if (dd < bestD)
        {
            bestD = dd;
            best  = k;
        }
    }

    if (best >= 0)
    {
        float u = (best + 0.5) / RegionCount;
        float v = 1.0 - (floor(PaletteRow) + 0.5) / max(PaletteRows, 1.0); // top row = PaletteRow 0
        OutColor = SAMPLE_TEXTURE2D_LOD(TargetPalette.tex, SS.samplerstate, float2(u, v), 0).rgb;
    }
    // best < 0  ->  OutColor stays InColor (fallback)
}

#endif

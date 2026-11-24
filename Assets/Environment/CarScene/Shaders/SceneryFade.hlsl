#ifndef CARSCENE_SCENERY_FADE_INCLUDED
#define CARSCENE_SCENERY_FADE_INCLUDED

// Set per renderer, once from the player camera. Shadows use that same value.
float _SceneryVisibility;

void ClipSceneryVisibility(float2 pixelPosition)
{
    if (_SceneryVisibility >= 1.0) return;
    if (_SceneryVisibility <= 0.0) { clip(-1.0); return; }
    float noise = frac(52.9829189 * frac(dot(floor(pixelPosition), float2(0.06711056, 0.00583715))));
    clip(_SceneryVisibility - noise);
}

#endif

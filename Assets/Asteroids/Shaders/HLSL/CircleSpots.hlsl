void CircleSpots_float(float3 UV, float minSize, float maxSize, int spotCount, float blur, out float Out) 
{
    float intensity = 0.0;
    float3 normalizedUV = normalize(UV);

    // Extended array of fixed offsets to increase variety
    float offsetArray[12] = { 
        0.05, -0.03, 0.08, -0.06, 0.1, -0.04, 
        0.07, -0.09, 0.02, 0.11, -0.05, 0.03 
    };

    for (int i = 0; i < spotCount; ++i) {
        // Determine spiral direction based on index parity. Its Fake random. Im couldn't adapt function rand3dTo3d without flickering
        float direction = (i % 2 == 0) ? 1.0 : -1.0; // -1 for even indices (reverse spiral)

        // Main spiral distribution considering direction
        float spotPhi = direction * (i * 2.4) * 2.0 * 3.14159265 / spotCount;  // Longitude
        float spotTheta = (i * 3.14159265) / (spotCount + 1);                  // Latitude

        // Add slight offset from offsetArray
        spotPhi += offsetArray[i % 12];
        spotTheta += offsetArray[(i * 2 + 5) % 12];  // Offset for variety

        // Calculate spot position on the sphere considering direction and offset
        float3 spotPosition = float3(
            sin(spotTheta) * cos(spotPhi),
            sin(spotTheta) * sin(spotPhi),
            cos(spotTheta)
        );

        // Spot size based on index `i`, distributed linearly
        float spotSize = lerp(minSize, maxSize, float(i) / float(spotCount - 1));

        // Calculate distance from current UV position to spot center
        float distToSpot = length(normalizedUV - spotPosition);

        // Add intensity blur from center to spot edges
        if (distToSpot < spotSize) {
            float t = distToSpot / spotSize;
            float spotIntensity = exp(-blur * t * t);
            intensity = max(intensity, spotIntensity);
        }
    }

    // Clamp final intensity to the range [0, 1]
    Out = saturate(intensity);
}

Shader "Hidden/HemiVeil"
{
    // only adds a neutral veil over the blind field.

    Properties
    {
        _VeilColor   ("Veil Colour (keep mid/bright; black is invisible on OST)", Color) = (0.5,0.5,0.5,1)
        _GazeUV      ("Gaze viewport point (set from script)", Vector) = (0.5,0.5,0,0)
        _FovXDeg     ("Horizontal FOV (deg, set from script)", Float) = 43
        _FovYDeg     ("Vertical FOV (deg, set from script)",   Float) = 29
        _SparingDeg  ("Macular sparing radius (deg)", Float) = 1.0
        _EdgeDeg     ("Boundary feather (deg)",       Float) = 1.5
        _Side        ("Blind side (+1 = right HH, -1 = left HH)", Float) = 1
        _MaxAlpha    ("Max veil opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off  ZWrite Off  ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _VeilColor;
            float4 _GazeUV;
            float  _FovXDeg, _FovYDeg, _SparingDeg, _EdgeDeg, _Side, _MaxAlpha;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // fragment position in screen space (0..1), independent of the quad's UVs/scale.
                float2 screenUV = i.pos.xy / _ScreenParams.xy;

                // angular offset of this fragment from the gaze point.
                float2 d        = screenUV - _GazeUV.xy;
                float  dxDeg    = d.x * _FovXDeg;
                float  dyDeg    = d.y * _FovYDeg;
                float  radialDeg= sqrt(dxDeg*dxDeg + dyDeg*dyDeg);

                float edge = max(_EdgeDeg, 1e-4);

                // hemifield veil, 0 at the vertical meridian through fixation, ramps to 1 into the
                // blind side over _EdgeDeg. _Side picks which side is blind.
                float hemi  = saturate((_Side * dxDeg) / edge);

                // Macular sparing (centre fixation clear)
                float spare = saturate((radialDeg - _SparingDeg) / edge);

                float a = hemi * spare * _MaxAlpha;

                return fixed4(_VeilColor.rgb, a);
            }
            ENDCG
        }
    }
}

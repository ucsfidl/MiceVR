// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Gradient" {
    Properties {
        _Color1 ("Color1", Color) = (1,1,1,1)
        _Color2 ("Color2", Color) = (0,0,0,1)
        _VFreq ("VFreq", Range (0,1000)) = 1
		_VPhase ("VPhase", Range (0, 359)) = 0
		_HFreq ("HFreq", Range (0,1000)) = 1
		_HPhase ("HPhase", Range (0, 359)) = 0
		_Deg ("Degrees", Range (-45,45)) = 0
		_Transparency("Transparency", Range(0.0, 1)) = 1
    }
	SubShader {
		Tags { "RenderType" = "Transparent"}

		Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			fixed4 _Color1;
			fixed4 _Color2;
			float _VFreq;
			float _HFreq;
			float _Deg;
			float _VPhase;
			float _HPhase;
			float _Transparency;

            struct vertexInput {
                float4 vertex : POSITION;
                float4 texcoord0 : TEXCOORD0;
            };

            struct fragmentInput{
                float4 position : SV_POSITION;
                float4 texcoord0 : TEXCOORD0;
            };

            fragmentInput vert(vertexInput i){
                fragmentInput o;
                o.position = UnityObjectToClipPos (i.vertex);
                o.texcoord0 = i.texcoord0;
                return o;
            }

            fixed4 frag(fragmentInput i) : SV_Target {
                fixed4 color;
				float degRad = _Deg / 180 * 3.14;
				float x = sin(((i.texcoord0.x-0.5)*cos(degRad) - (i.texcoord0.y-0.5)*sin(degRad)) * _VFreq*2*3.14 + _VPhase/180*3.14);
				float y = sin(((i.texcoord0.x-0.5)*sin(degRad) + (i.texcoord0.y-0.5)*cos(degRad)) * _HFreq*2*3.14 + _HPhase/180*3.14);

				if (x >= 0 ) {
					if (y >= 0) {
						color = _Color1;
					} else {
						color = _Color2;
					}
				} else {
					if (y >= 0) {
						color = _Color2;
					} else {
						color = _Color1;
					}
				}

				color.a = _Transparency;
                return color;
            }
            ENDCG
        }
    }
	Fallback "VertexLit"
}
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Gradient" {
    Properties {
        _Color1 ("Color1", Color) = (1,1,1,1)
        _Color2 ("Color2", Color) = (0,0,0,0)
        _VFreq ("VFreq", Range (1,1000)) = 1
		_HFreq ("HFreq", Range (1,1000)) = 1
		_Deg ("Degrees", Range (-45,45)) = 0
    }
	SubShader {
		Tags { "RenderType" = "Opaque"}
		Lighting On
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
				if( _Deg >= 0 )
				{
					if ( fmod((i.texcoord0.x+((_Deg/45.0)*i.texcoord0.y))*_VFreq,2.0) < 1.0 ){
						if ( fmod(i.texcoord0.y*_HFreq,2.0) < 1.0 )
						{
							color = _Color1;
						} else {
							color = _Color2;
						}
					} else {
						if ( fmod(i.texcoord0.y*_HFreq,2.0) > 1.0 )
						{
							color = _Color1;
						} else {
							color = _Color2;
						}
					}
				}
				else
				{
				if ( fmod(((1.0+i.texcoord0.x)+((_Deg/45.0)*i.texcoord0.y))*_VFreq,2.0) < 1.0 ){
						if ( fmod(i.texcoord0.y*_HFreq,2.0) < 1.0 )
						{
							color = _Color1;
						} else {
							color = _Color2;
						}
					} else {
						if ( fmod(i.texcoord0.y*_HFreq,2.0) > 1.0 )
						{
							color = _Color1;
						} else {
							color = _Color2;
						}
					}
				}
                return color;
            }
            ENDCG
        }
    }
	Fallback "VertexLit"
}
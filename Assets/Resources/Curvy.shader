// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Curvy" {
    Properties {
        _Color1 ("Color1", Color) = (1,1,1,1)
        _Color2 ("Color2", Color) = (0,0,0,0)
        _VFreq ("VFreq", Range (0,1000)) = 1
		_VPhase ("VPhase", Range (0, 359)) = 0
		_HFreq ("HFreq", Range (0,1000)) = 1
		_HPhase ("HPhase", Range (0, 359)) = 0
		_Deg ("Degrees", Range (-45,45)) = 0
		_Smooth ("Smooth", Range(0,1)) = 1
		_VAmplitude ("VAmplitude", Range(0, 100)) = 0
		_VNumCycles ("VNumCycles", Range(0, 100)) = 0 
		_VWavePhase ("VWavePhase", Range(-3.14, 3.14)) = -1.35
		_HAmplitude ("HAmplitude", Range(0, 100)) = 0
		_HNumCycles ("HNumCycles", Range(0, 100)) = 0 
		_HWavePhase ("HWavePhase", Range(-3.14, 3.14)) = 0
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
			float _VPhase;
			float _HFreq;
			float _HPhase;
			float _Deg;
			float _Smooth;
			float _VAmplitude;
			float _VNumCycles;
			float _VWavePhase;
			float _HAmplitude;
			float _HNumCycles;
			float _HWavePhase;

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
				if( _Smooth == 0 )
				{
					float P = 0.15 / _VNumCycles * 3.1;  // P is the half period
					float VA =_VAmplitude*2; //_VAmplitude * 2;
					float PI = 3.1415926535;
					float w = _VNumCycles * 2.1;
					color = _Color1;
					//if ( fmod((i.texcoord0.x + (_VAmplitude*2 / (_VNumCycles/10)) * (_VNumCycles/10 - abs(fmod(i.texcoord0.y, 2*_VNumCycles/10) - _VNumCycles/10)))*_VFreq + 0.35, 2.0) <= 1.0 ){
					//if ( fmod((i.texcoord0.x + (VA/P) * (P - abs(fmod(i.texcoord0.y, 2*P) - P)))*_VFreq - 5.333*VA + 6.65, 2.0) < 1.0 ){
					//if ( fmod((i.texcoord0.x + (1-2*VA*abs(1./VA - frac(1./VA * i.texcoord0.y + 1./(2*VA)))))*_VFreq + 0.5, 2.0) < 1.0 ){
					//if ( fmod((i.texcoord0.x + (1-VA*abs(1 - (VA*((1./2*i.texcoord0.y + 1./4) % 1)))))*_VFreq + 0.5, 2.0) < 1.0 ){
					if ( fmod((i.texcoord0.x + (VA/PI*asin(sin(PI*i.texcoord0.y*w + _VWavePhase))))*_VFreq + (_VPhase+360)/180 + 0.35, 2.0) < 1.0 ){
						if ( fmod((i.texcoord0.y + _HWavePhase)*_HFreq + (_HPhase+360)/180,2.0) < 1.0 )
						{
							color = _Color1;
						} else {
							color = _Color2;
						}
					} else {
						if ( fmod((i.texcoord0.y + _HWavePhase)*_HFreq + (_HPhase+360)/180,2.0) > 1.0 )
						{
							color = _Color1;
						} else {
							color = _Color2;
						}
					}
				}
				else if (_Smooth == 1)
				{
					if ( fmod((i.texcoord0.x + _VAmplitude * sin(_VNumCycles/0.15*i.texcoord0.y + _VWavePhase))*_VFreq + (_VPhase+360)/180 + 0.35,2.0) < 1.0 ){
						if ( fmod((i.texcoord0.y + _HAmplitude * sin(_HNumCycles*i.texcoord0.x + _HWavePhase))*_HFreq + (_HPhase+360)/180,2.0) < 1.0 )
						{
							color = _Color1;
						} else {
							color = _Color2;
						}
					} else {
						if ( fmod((i.texcoord0.y + _HWavePhase)*_HFreq + (_HPhase+360)/180,2.0) > 1.0 )
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
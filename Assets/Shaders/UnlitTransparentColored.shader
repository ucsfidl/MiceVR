Shader "Unlit/Transparent Colored" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader {
		Tags {"Queue" = "Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

		Lighting Off
		Zwrite Off
		Fog {Mode Off}

		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			Color [_Color]
			SetTexture [_MainTex] { combine texture * primary}
		}
	}
}
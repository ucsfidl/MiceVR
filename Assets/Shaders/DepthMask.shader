Shader "Masked/Mask" {
	SubShader {
		// Render the mask after regular geometry, but before masked geometry and transparent things

		Tags {"Queue" = "Transparent-10" }

		// Dont' draw in the RGBA channels; judt the depth buffer

		ColorMask 0
		Zwrite On

		// Do nothing specific in the pass:

		Pass {}
	}
}
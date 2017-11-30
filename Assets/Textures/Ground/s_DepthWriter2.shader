Shader "Unlit/s_DepthWriter2"
{
	SubShader
	{
		Tags { "Queue" = "Geometry+10" }

		ColorMask 0
		ZWrite On
		Pass{}
	}
}

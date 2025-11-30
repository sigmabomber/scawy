Shader "Retro 3D Shader Pack/Unity Lit"
{ 
	Properties              
	{      
		_MainTex("Albedo Texture", 2D) = "white" {}
_Color("Albedo Color Tint", Color) = (1, 1, 1, 1)
_SpecGlossMap("Specular Map", 2D) = "white" {}
_SpecularColor("Specular Color", Color) = (0, 0, 0, 1)
_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.0
_BumpMap("Normal Map", 2D) = "bump" {}
[HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
[HDR] _EmissionMap("Emission Map", 2D) = "black" {}

_VertJitter("Vertex Jitter", Range(0.0, 0.999)) = 0.95
_AffineMapIntensity("Affine Texture Mapping Intensity", Range(0.0, 1.0)) = 1.0
_DrawDist("Draw Distance", Float) = 0
	}
		
	SubShader
	{		
		Tags { "RenderType" = "Opaque" }			
  		 		 
		CGPROGRAM
					
		#pragma surface surf StandardSpecular vertex:vert 
		#pragma target 3.0		
		#pragma shader_feature_local ENABLE_SCREENSPACE_JITTER
		#pragma shader_feature_local USING_SPECULAR_MAP // Whether the shader is using the specular map or specular color.
		#pragma shader_feature_local EMISSION_ENABLED 
		#pragma shader_feature_local USING_EMISSION_MAP 
		#include "./CG_Includes/RetroUnityLit.cginc" // The include file containing the majority of the shader code which is shared between the transparent and non-transparent variants of the shader. 	
			 
		ENDCG
	}

	FallBack "Diffuse"
	CustomEditor "RetroUnityLitShaderCustomGUI"
}

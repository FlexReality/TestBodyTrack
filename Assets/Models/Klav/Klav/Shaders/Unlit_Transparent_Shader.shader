// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom_FlexReality/Unlit/Unlit_Transparent_Shader/Unlit_Transparent_Shader"
{
	Properties
	{
		_Texture("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,0)
		_AlphaTexture("Alpha Texture", 2D) = "white" {}
		_Opacity("Opacity", Range( 0 , 1)) = 1
		[KeywordEnum(Use_texture,Use_alfa_channel)] _Opacitymode("Opacity mode", Float) = 0
		[KeywordEnum(Fade,Cutout)] _Opacitytype("Opacity type", Float) = 0
		_Cutout_Treshold("Cutout_Treshold", Range( 0 , 1)) = 0.5
		[Enum(Back,2,Front,1,Off,0)]_Cullmode("Cull mode", Float) = 2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Overlay+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull [_Cullmode]
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha , SrcAlpha OneMinusSrcAlpha
		
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _OPACITYTYPE_FADE _OPACITYTYPE_CUTOUT
		#pragma shader_feature_local _OPACITYMODE_USE_TEXTURE _OPACITYMODE_USE_ALFA_CHANNEL
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _Cullmode;
		uniform float4 _Color;
		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform sampler2D _AlphaTexture;
		uniform float4 _AlphaTexture_ST;
		uniform float _Opacity;
		uniform float _Cutout_Treshold;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			float4 tex2DNode3 = tex2D( _Texture, uv_Texture );
			o.Emission = ( _Color * tex2DNode3 ).rgb;
			float2 uv_AlphaTexture = i.uv_texcoord * _AlphaTexture_ST.xy + _AlphaTexture_ST.zw;
			float4 temp_cast_1 = (tex2DNode3.a).xxxx;
			#if defined(_OPACITYMODE_USE_TEXTURE)
				float4 staticSwitch11 = tex2D( _AlphaTexture, uv_AlphaTexture );
			#elif defined(_OPACITYMODE_USE_ALFA_CHANNEL)
				float4 staticSwitch11 = temp_cast_1;
			#else
				float4 staticSwitch11 = tex2D( _AlphaTexture, uv_AlphaTexture );
			#endif
			float4 temp_output_9_0 = ( staticSwitch11 * _Opacity );
			float4 temp_cast_2 = (1.0).xxxx;
			#if defined(_OPACITYTYPE_FADE)
				float4 staticSwitch20 = temp_output_9_0;
			#elif defined(_OPACITYTYPE_CUTOUT)
				float4 staticSwitch20 = temp_cast_2;
			#else
				float4 staticSwitch20 = temp_output_9_0;
			#endif
			o.Alpha = staticSwitch20.r;
			float4 temp_cast_4 = (1.0).xxxx;
			float4 temp_cast_5 = (1.0).xxxx;
			#if defined(_OPACITYTYPE_FADE)
				float4 staticSwitch21 = temp_cast_4;
			#elif defined(_OPACITYTYPE_CUTOUT)
				float4 staticSwitch21 = temp_output_9_0;
			#else
				float4 staticSwitch21 = temp_cast_4;
			#endif
			clip( staticSwitch21.r - _Cutout_Treshold );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Unlit keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				SurfaceOutput o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutput, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18900
1920;0;1920;1019;350.7896;236.0795;1.142185;True;True
Node;AmplifyShaderEditor.SamplerNode;3;-761.4167,-105.2085;Inherit;True;Property;_Texture;Texture;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;7;-769.5385,234.364;Inherit;True;Property;_AlphaTexture;Alpha Texture;2;0;Create;True;0;0;0;False;0;False;-1;None;71b1a28028d298746b6baecdf1dfd518;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;8;-763.9285,517.2181;Inherit;False;Property;_Opacity;Opacity;3;0;Create;True;0;0;0;False;0;False;1;0.76;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;11;-178.7955,146.5648;Inherit;False;Property;_Opacitymode;Opacity mode;4;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;2;Use_texture;Use_alfa_channel;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;4;-680.4167,-291.2085;Inherit;False;Property;_Color;Color;1;0;Create;True;0;0;0;False;0;False;1,1,1,0;0.09659999,0.4427501,0.69,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;214.4671,349.2433;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;22;304.3012,64.47294;Inherit;False;Constant;_Float0;Float 0;8;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-243.4702,-185.4239;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;20;590.7324,17.21342;Inherit;False;Property;_Opacitytype;Opacity type;5;0;Create;False;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;2;Fade;Cutout;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;21;597.0645,207.2493;Inherit;False;Property;_Opacitytype;Opacity type;7;0;Create;False;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;2;Fade;Cutout;Create;True;True;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;23;965.0079,353.2881;Inherit;False;Property;_Cutout_Treshold;Cutout_Treshold;6;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;979.8561,533.7534;Inherit;False;Property;_Cullmode;Cull mode;7;1;[Enum];Create;True;0;3;Back;2;Front;1;Off;0;0;True;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;2;1000.816,-169.8858;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Custom_FlexReality/Unlit_Transparent_Shader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;2;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Transparent;;Overlay;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;2;5;False;-1;10;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;True;24;-1;0;True;23;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;11;1;7;0
WireConnection;11;0;3;4
WireConnection;9;0;11;0
WireConnection;9;1;8;0
WireConnection;5;0;4;0
WireConnection;5;1;3;0
WireConnection;20;1;9;0
WireConnection;20;0;22;0
WireConnection;21;1;22;0
WireConnection;21;0;9;0
WireConnection;2;2;5;0
WireConnection;2;9;20;0
WireConnection;2;10;21;0
ASEEND*/
//CHKSM=239EDA25D126CA2262071EA7A3917B4AAE4E30C9
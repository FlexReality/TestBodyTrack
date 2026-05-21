// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/PixelScreen_v2"
{
	Properties
	{
		[IntRange]_Frame("Frame", Range( 0 , 15)) = 0
		_Moveyesx("Mov eyes x", Range( -0.4 , 0.4)) = 0
		_Moveyesy("Mov eyes y", Range( -0.4 , 0.4)) = 0
		_Eyeopacity("Eye opacity", Range( 0 , 1)) = 0
		[HDR]_Color("Color", Color) = (1,1,1,0)
		_Pixelize("Pixelize", Float) = 64
		_Scale("Scale", Float) = 1
		_Offset("Offset", Vector) = (0,0,0,0)
		_Eyeposition("Eye position", Vector) = (0,0,0,0)
		_EyeScale("Eye Scale", Vector) = (1.8,3.6,0,0)
		[NoScaleOffset]_Animatlas("Anim atlas", 2D) = "white" {}
		[NoScaleOffset][SingleLineTexture]_PixelMask("PixelMask", 2D) = "white" {}
		_Eyestex("Eyes tex", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float4 _Color;
		uniform float2 _Offset;
		uniform float _Scale;
		uniform sampler2D _Animatlas;
		uniform float _Frame;
		uniform sampler2D _Eyestex;
		uniform float2 _EyeScale;
		uniform float2 _Eyeposition;
		uniform float _Moveyesx;
		uniform float _Moveyesy;
		uniform float _Eyeopacity;
		uniform sampler2D _PixelMask;
		uniform float _Pixelize;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 color37 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			o.Albedo = color37.rgb;
			float2 temp_cast_1 = (0.5).xx;
			float2 temp_output_7_0 = ( i.uv_texcoord - temp_cast_1 );
			float2 temp_output_9_0 = ( ( ( temp_output_7_0 + _Offset ) * _Scale ) + 0.5 );
			float2 appendResult10_g2 = (float2(0.98 , 0.98));
			float2 temp_output_11_0_g2 = ( abs( (temp_output_9_0*2.0 + -1.0) ) - appendResult10_g2 );
			float2 break16_g2 = ( 1.0 - ( temp_output_11_0_g2 / fwidth( temp_output_11_0_g2 ) ) );
			float2 temp_cast_2 = (0.5).xx;
			float temp_output_4_0_g3 = 4.0;
			float temp_output_5_0_g3 = 4.0;
			float2 appendResult7_g3 = (float2(temp_output_4_0_g3 , temp_output_5_0_g3));
			float totalFrames39_g3 = ( temp_output_4_0_g3 * temp_output_5_0_g3 );
			float2 appendResult8_g3 = (float2(totalFrames39_g3 , temp_output_5_0_g3));
			float clampResult42_g3 = clamp( 0.0 , 0.0001 , ( totalFrames39_g3 - 1.0 ) );
			float temp_output_35_0_g3 = frac( ( ( _Frame + clampResult42_g3 ) / totalFrames39_g3 ) );
			float2 appendResult29_g3 = (float2(temp_output_35_0_g3 , ( 1.0 - temp_output_35_0_g3 )));
			float2 temp_output_15_0_g3 = ( ( temp_output_9_0 / appendResult7_g3 ) + ( floor( ( appendResult8_g3 * appendResult29_g3 ) ) / appendResult7_g3 ) );
			float2 temp_cast_3 = (0.5).xx;
			float2 appendResult43 = (float2(_Moveyesx , _Moveyesy));
			float4 lerpResult33 = lerp( tex2D( _Eyestex, ( ( ( temp_output_7_0 * _EyeScale ) + _Eyeposition + appendResult43 ) + 0.5 ) ) , float4( 1,1,1,0 ) , _Eyeopacity);
			float2 temp_cast_4 = (0.5).xx;
			o.Emission = ( _Color * ( saturate( min( break16_g2.x , break16_g2.y ) ) * ( ( tex2D( _Animatlas, temp_output_15_0_g3 ) * lerpResult33 ) * ( 1.0 - tex2D( _PixelMask, ( temp_output_9_0 * _Pixelize ) ) ) ) ) ).rgb;
			o.Metallic = 1.0;
			o.Smoothness = 0.2;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-477.0248,280.002;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;15;-226.9424,236.1412;Inherit;True;Property;_PixelMask;PixelMask;11;2;[NoScaleOffset];[SingleLineTexture];Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;1936.37,-301.5082;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;1374.97,-221.8195;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;873.6777,-116.1302;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;33;518.9255,295.3815;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;519.0564,-111.7524;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;2;49.42451,-96.37405;Inherit;True;Property;_Animatlas;Anim atlas;10;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;34;165.8624,416.4229;Inherit;False;Property;_Eyeopacity;Eye opacity;3;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;2378.274,-87.17999;Inherit;False;Constant;_Float1;Float 1;11;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;36;2355.274,-178.18;Inherit;False;Constant;_Float3;Float 3;11;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2637.901,-309.3193;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Hidden/PixelScreen_v2;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.ColorNode;37;2335.274,-457.18;Inherit;False;Constant;_Color0;Color 0;11;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;38;194.3261,187.3966;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;18;1589.362,-618.9099;Inherit;False;Property;_Color;Color;4;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;14;-665.8341,359.035;Inherit;False;Property;_Pixelize;Pixelize;5;0;Create;True;0;0;0;False;0;False;64;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-1836.636,35.66929;Inherit;False;Constant;_Float2;Float 2;3;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;1;-310.7962,-147.7541;Inherit;False;Flipbook;-1;;3;53c2488c220f6564ca6c90721ee16673;2,71,0,68,0;8;51;SAMPLER2D;0.0;False;13;FLOAT2;0,0;False;4;FLOAT;4;False;5;FLOAT;4;False;24;FLOAT;0;False;2;FLOAT;0;False;55;FLOAT;0;False;70;FLOAT;0;False;5;COLOR;53;FLOAT2;0;FLOAT;47;FLOAT;48;FLOAT;62
Node;AmplifyShaderEditor.RangedFloatNode;3;-611.371,-54.40653;Inherit;False;Property;_Frame;Frame;0;1;[IntRange];Create;True;0;0;0;False;0;False;0;0;0;15;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;12;-143.1627,-547.7869;Inherit;False;Rectangle;-1;;2;6b23e0c975270fb4084c354b2c83366a;0;3;1;FLOAT2;0,0;False;2;FLOAT;0.98;False;3;FLOAT;0.98;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-2019.972,-217.4933;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;7;-1663.185,-228.6828;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-1042.96,-172.7785;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-1225.013,-228.918;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-1386.499,-136.8374;Inherit;False;Property;_Scale;Scale;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;-1416.448,-273.6894;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;40;-1627.448,-102.6894;Inherit;False;Property;_Offset;Offset;7;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;21;92.0592,747.1113;Inherit;True;Property;_Eyestex;Eyes tex;12;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-338.4721,827.0557;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;25;-617.904,910.7886;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;22;-1069.927,932.7035;Inherit;False;Property;_EyeScale;Eye Scale;9;0;Create;True;0;0;0;False;0;False;1.8,3.6;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-849.7128,831.0664;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;24;-892.7476,1019.628;Inherit;False;Property;_Eyeposition;Eye position;8;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;43;-724.1493,1262.049;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-1010.939,1226.712;Inherit;False;Property;_Moveyesx;Mov eyes x;1;0;Create;True;0;0;0;False;0;False;0;0;-0.4;0.4;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-1009.938,1313.012;Inherit;False;Property;_Moveyesy;Mov eyes y;2;0;Create;True;0;0;0;False;0;False;0;0;-0.4;0.4;0;1;FLOAT;0
WireConnection;17;0;9;0
WireConnection;17;1;14;0
WireConnection;15;1;17;0
WireConnection;19;0;18;0
WireConnection;19;1;10;0
WireConnection;10;0;12;0
WireConnection;10;1;16;0
WireConnection;16;0;20;0
WireConnection;16;1;38;0
WireConnection;33;0;21;0
WireConnection;33;2;34;0
WireConnection;20;0;2;0
WireConnection;20;1;33;0
WireConnection;2;1;1;0
WireConnection;0;0;37;0
WireConnection;0;2;19;0
WireConnection;0;3;36;0
WireConnection;0;4;35;0
WireConnection;38;0;15;0
WireConnection;1;13;9;0
WireConnection;1;2;3;0
WireConnection;12;1;9;0
WireConnection;7;0;4;0
WireConnection;7;1;8;0
WireConnection;9;0;5;0
WireConnection;9;1;8;0
WireConnection;5;0;39;0
WireConnection;5;1;6;0
WireConnection;39;0;7;0
WireConnection;39;1;40;0
WireConnection;21;1;26;0
WireConnection;26;0;25;0
WireConnection;26;1;8;0
WireConnection;25;0;23;0
WireConnection;25;1;24;0
WireConnection;25;2;43;0
WireConnection;23;0;7;0
WireConnection;23;1;22;0
WireConnection;43;0;41;0
WireConnection;43;1;42;0
ASEEND*/
//CHKSM=0CD84C5120A4196AF976258328080FDF29B6A727
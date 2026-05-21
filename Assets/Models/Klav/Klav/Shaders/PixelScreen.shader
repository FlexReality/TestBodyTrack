// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/PixelScreen"
{
	Properties
	{
		_Float7("Float 7", Float) = 128
		[HDR]_Color0("Color 0", Color) = (0,0,0,0)
		_Eyeposition("Eye position", Vector) = (0,0,0,0)
		_EyeLidposition("EyeLid position", Vector) = (0,0,0,0)
		_PixelMask("PixelMask", 2D) = "white" {}
		_Nouse("Nouse", 2D) = "white" {}
		_Mouth("Mouth", 2D) = "white" {}
		_Eyesize("Eye size", Float) = 1
		_EyeLidsize("Eye Lid size", Float) = 1
		_Masksize("Mask size", Float) = 1
		_Mouthpos("Mouth pos", Vector) = (0,0,0,0)
		_Nousepos("Nouse pos", Vector) = (0,0,0,0)
		_Mouthsize("Mouth size", Float) = 1
		_Nousesize("Nouse size", Float) = 1
		_Blink("Blink", Range( 0 , 1)) = 1
		_Texture0("Texture 0", 2D) = "white" {}
		_Vector10("Vector 10", Vector) = (0,0,0,0)
		_Emissmask("Emiss mask", 2D) = "white" {}
		_Intency("Intency", Float) = 1
		_EyeLid("EyeLid", 2D) = "white" {}
		_LookPos("LookPos", Vector) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _PixelMask;
		uniform float _Float7;
		uniform sampler2D _Texture0;
		uniform float _Eyesize;
		uniform float _Blink;
		uniform float2 _Eyeposition;
		uniform sampler2D _EyeLid;
		uniform float2 _LookPos;
		uniform float _EyeLidsize;
		uniform float2 _EyeLidposition;
		uniform float _Masksize;
		uniform float2 _Vector10;
		uniform sampler2D _Nouse;
		uniform float2 _Nousepos;
		uniform float _Nousesize;
		uniform sampler2D _Mouth;
		uniform float2 _Mouthpos;
		uniform float _Mouthsize;
		uniform sampler2D _Emissmask;
		uniform float4 _Emissmask_ST;
		uniform float _Intency;
		uniform float4 _Color0;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 temp_cast_0 = (_Float7).xx;
			float2 uv_TexCoord124 = i.uv_texcoord * temp_cast_0;
			float pixelWidth107 =  1.0f / _Float7;
			float pixelHeight107 = 1.0f / _Float7;
			half2 pixelateduv107 = half2((int)(i.uv_texcoord.x / pixelWidth107) * pixelWidth107, (int)(i.uv_texcoord.y / pixelHeight107) * pixelHeight107);
			float2 _Vector11 = float2(0.5,0);
			float2 temp_cast_1 = (0.25).xx;
			float2 appendResult133 = (float2(1.0 , (1.0 + (_Blink - 0.0) * (10.0 - 1.0) / (1.0 - 0.0))));
			float2 temp_cast_2 = (0.25).xx;
			float2 temp_cast_3 = (0.25).xx;
			float2 appendResult152 = (float2(1.0 , (1.0 + (_Blink - 0.0) * (10.0 - 1.0) / (1.0 - 0.0))));
			float2 temp_cast_4 = (0.5).xx;
			float2 temp_cast_5 = (0.5).xx;
			float2 uv_Emissmask = i.uv_texcoord * _Emissmask_ST.xy + _Emissmask_ST.zw;
			o.Emission = ( ( tex2D( _PixelMask, uv_TexCoord124 ) * ( saturate( ( ( ( ( tex2D( _Texture0, ( ( ( ( ( abs( ( pixelateduv107 - _Vector11 ) ) - temp_cast_1 ) * _Eyesize ) * appendResult133 ) + 0.25 ) + _Eyeposition ) ) * tex2D( _EyeLid, ( ( ( ( abs( ( _LookPos - pixelateduv107 ) ) - temp_cast_2 ) * _EyeLidsize ) + 0.25 ) + _EyeLidposition ) ) ) * ( 1.0 - tex2D( _Texture0, ( ( ( ( ( ( abs( ( pixelateduv107 - _Vector11 ) ) - temp_cast_3 ) * ( _Eyesize * _Masksize ) ) * appendResult152 ) + 0.25 ) + _Eyeposition ) + _Vector10 ) ) ) ) + tex2D( _Nouse, ( ( ( ( pixelateduv107 + _Nousepos ) - temp_cast_4 ) * _Nousesize ) + 0.5 ) ) ) + tex2D( _Mouth, ( ( ( ( pixelateduv107 + _Mouthpos ) - temp_cast_5 ) * _Mouthsize ) + 0.5 ) ) ) ) + ( ( 1.0 - tex2D( _Emissmask, uv_Emissmask ) ) * _Intency ) ) ) * _Color0 ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.Vector2Node;113;2952.124,2834.863;Inherit;False;Property;_Vector13;Vector 13;7;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ScaleAndOffsetNode;112;3184.377,2803.312;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCPixelate;107;1974.416,1000.556;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;64;False;2;FLOAT;64;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;94;1723.829,1000.803;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;155;2584.101,3170.434;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;111;5351.992,1003.026;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;125;4190.234,2586.393;Inherit;False;Constant;_Float8;Float 6;32;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;126;4351.16,2500.842;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;127;4493.58,2502.682;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;128;4638,2503.653;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;129;4387.285,2640.764;Inherit;False;Property;_Nousesize;Nouse size;14;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;110;4964.472,2487.125;Inherit;True;Property;_Nouse;Nouse;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;114;4580.171,2711.841;Inherit;False;Property;_Nousepos;Nouse pos;12;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;130;4106.818,2388.286;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;160;4242.677,3390.061;Inherit;False;Constant;_Float10;Float 6;32;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;161;4403.603,3304.51;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;162;4546.023,3306.35;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;163;4690.443,3307.321;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;165;5016.915,3290.793;Inherit;True;Property;_Mouth;Mouth;6;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;167;4159.261,3191.954;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;166;4632.614,3515.509;Inherit;False;Property;_Mouthpos;Mouth pos;11;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;164;4439.728,3444.432;Inherit;False;Property;_Mouthsize;Mouth size;13;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;168;2306.08,3383.939;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;169;5734.464,1057.351;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;170;5873.583,1030.943;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;7371.246,687.2996;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Hidden/PixelScreen;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.RangedFloatNode;175;6466.164,1298.642;Inherit;False;Property;_Intency;Intency;21;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;171;6056.761,1268.495;Inherit;True;Property;_Emissmask;Emiss mask;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;173;6368.912,1179.493;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;174;6594.644,1121.159;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;73;6971.177,641.2762;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;172;6508.706,845.2084;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;109;6673.03,775.0419;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;138;4705.622,1605.791;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;137;4387.583,1593.613;Inherit;True;Property;_TextureSample5;Texture Sample 5;33;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;141;3926.101,1708.645;Inherit;False;Property;_Vector9;Vector 9;18;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;142;3908.508,1846.685;Inherit;False;Property;_Vector10;Vector 10;19;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;156;3941.927,1554.953;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;140;4126.568,1837.499;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;159;4177.29,1598.416;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;143;2921.044,1978.61;Inherit;False;Constant;_Float9;Float 6;32;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;144;3081.967,1893.058;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;145;3224.385,1894.898;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;148;2947.1,1896.487;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;149;2763.394,1891.556;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;150;3577.607,1876.269;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;151;3388.04,1889.023;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;152;3279.935,2123.76;Inherit;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;153;3104.935,2147.76;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;146;2807.791,2119.684;Inherit;False;Property;_Masksize;Mask size;10;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;158;3110.717,2029.595;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;136;4043.838,872.1753;Inherit;True;Property;_Texture0;Texture 0;17;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;118;2968.881,977.1667;Inherit;False;Constant;_Float6;Float 6;32;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;117;3129.804,891.6149;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;119;3272.222,893.4551;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;120;3165.929,1031.538;Inherit;False;Property;_Eyesize;Eye size;8;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;97;2994.937,895.0432;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;99;2811.231,890.1124;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;121;3625.445,874.8256;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;3435.877,887.5793;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;133;3327.772,1122.316;Inherit;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;98;2530.747,1097.469;Inherit;False;Constant;_Vector11;Vector 11;25;0;Create;True;0;0;0;False;0;False;0.5,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;106;3650.521,1137.75;Inherit;False;Property;_Eyeposition;Eye position;2;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;134;2787.213,1135.168;Inherit;False;Property;_Blink;Blink;16;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;135;3111.585,1147.645;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;105;3937.756,1096.467;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;74;5877.078,-240.2486;Inherit;False;Property;_Color0;Color 0;1;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;124;5658.06,34.89193;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;108;6008.118,5.059622;Inherit;True;Property;_PixelMask;PixelMask;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;177;4296.179,92.69466;Inherit;True;Property;_EyeLid;EyeLid;22;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;185;3241.286,-716.8013;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;186;3091.579,-704.3647;Inherit;False;FLOAT2;4;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;189;2551.02,-691.5128;Inherit;False;Property;_Blink1;Blink;15;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;190;2875.392,-679.0358;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;187;1967.842,-43.58569;Inherit;False;Constant;_Vector12;Vector 11;25;0;Create;True;0;0;0;False;0;False;0.5,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;5011.093,1070.526;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;93;4412.55,1039.473;Inherit;True;Property;_Eye;Eye;23;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;193;4808.761,1026.052;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector2Node;188;3600.971,190.9716;Inherit;False;Property;_EyeLidposition;EyeLid position;3;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;123;1532.117,749.2114;Inherit;False;Property;_Float7;Float 7;0;0;Create;True;0;0;0;False;0;False;128;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;183;2374.998,-72.94732;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;180;2994.236,-306.0813;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;179;2837.151,-306.0086;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;181;2981.619,14.06678;Inherit;False;Property;_EyeLidsize;Eye Lid size;9;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;178;2580.786,-31.93649;Inherit;False;Constant;_Float11;Float 6;32;0;Create;True;0;0;0;False;0;False;0.25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;191;3865.363,-16.44441;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;184;3408.542,-245.9696;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0.05;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;195;2433.141,302.3862;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;182;2573.313,161.7021;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;196;1952.1,270.2179;Inherit;False;Property;_LookPos;LookPos;23;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
WireConnection;112;1;113;0
WireConnection;107;0;94;0
WireConnection;107;1;123;0
WireConnection;107;2;123;0
WireConnection;155;0;107;0
WireConnection;111;0;139;0
WireConnection;111;1;110;0
WireConnection;126;0;130;0
WireConnection;126;1;125;0
WireConnection;127;0;126;0
WireConnection;127;1;129;0
WireConnection;128;0;127;0
WireConnection;128;1;125;0
WireConnection;110;1;128;0
WireConnection;130;0;155;0
WireConnection;130;1;114;0
WireConnection;161;0;167;0
WireConnection;161;1;160;0
WireConnection;162;0;161;0
WireConnection;162;1;164;0
WireConnection;163;0;162;0
WireConnection;163;1;160;0
WireConnection;165;1;163;0
WireConnection;167;0;168;0
WireConnection;167;1;166;0
WireConnection;168;0;107;0
WireConnection;169;0;111;0
WireConnection;169;1;165;0
WireConnection;170;0;169;0
WireConnection;0;2;73;0
WireConnection;173;0;171;0
WireConnection;174;0;173;0
WireConnection;174;1;175;0
WireConnection;73;0;109;0
WireConnection;73;1;74;0
WireConnection;172;0;170;0
WireConnection;172;1;174;0
WireConnection;109;0;108;0
WireConnection;109;1;172;0
WireConnection;138;0;137;0
WireConnection;137;0;136;0
WireConnection;137;1;159;0
WireConnection;156;0;150;0
WireConnection;156;1;106;0
WireConnection;140;1;141;0
WireConnection;140;2;142;0
WireConnection;159;0;156;0
WireConnection;159;1;142;0
WireConnection;144;0;148;0
WireConnection;144;1;143;0
WireConnection;145;0;144;0
WireConnection;145;1;158;0
WireConnection;148;0;149;0
WireConnection;149;0;107;0
WireConnection;149;1;98;0
WireConnection;150;0;151;0
WireConnection;150;1;143;0
WireConnection;151;0;145;0
WireConnection;151;1;152;0
WireConnection;152;1;153;0
WireConnection;153;0;134;0
WireConnection;158;0;120;0
WireConnection;158;1;146;0
WireConnection;117;0;97;0
WireConnection;117;1;118;0
WireConnection;119;0;117;0
WireConnection;119;1;120;0
WireConnection;97;0;99;0
WireConnection;99;0;107;0
WireConnection;99;1;98;0
WireConnection;121;0;131;0
WireConnection;121;1;118;0
WireConnection;131;0;119;0
WireConnection;131;1;133;0
WireConnection;133;1;135;0
WireConnection;135;0;134;0
WireConnection;105;0;121;0
WireConnection;105;1;106;0
WireConnection;124;0;123;0
WireConnection;108;1;124;0
WireConnection;177;1;191;0
WireConnection;185;1;186;0
WireConnection;186;1;190;0
WireConnection;190;0;189;0
WireConnection;139;0;193;0
WireConnection;139;1;138;0
WireConnection;93;0;136;0
WireConnection;93;1;105;0
WireConnection;193;0;93;0
WireConnection;193;1;177;0
WireConnection;183;0;107;0
WireConnection;183;1;187;0
WireConnection;180;0;179;0
WireConnection;180;1;181;0
WireConnection;179;0;182;0
WireConnection;179;1;178;0
WireConnection;191;0;184;0
WireConnection;191;1;188;0
WireConnection;184;0;180;0
WireConnection;184;1;178;0
WireConnection;195;0;196;0
WireConnection;195;1;107;0
WireConnection;182;0;195;0
ASEEND*/
//CHKSM=2B03D2B1D920F064911F60C5E53876DB55E059D4
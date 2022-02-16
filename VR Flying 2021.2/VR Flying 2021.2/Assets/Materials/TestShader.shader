// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TestShader"
{
	Properties
	{
		_Grass1tile("Grass 1 tile", Float) = 2
		_SlopeMax("Slope Max", Float) = 1
		_SlopeMin("Slope Min", Float) = -1
		_Rock1tile("Rock 1 tile", Float) = 1
		_Rock("Rock", 2D) = "white" {}
		_Grass1("Grass 1", 2D) = "white" {}
		_ArialTreeTexture("Arial Tree Texture", 2D) = "white" {}
		_ArialTreeTile("Arial Tree Tile", Float) = 0
		_AntiTilingTexture("Anti Tiling Texture", 2D) = "white" {}
		_AntiTilingVector("Anti Tiling Vector", Vector) = (0.9,0.9,0.9,0)
		_GrassAntiTiling("Grass Anti Tiling", Vector) = (0,0,0,0)
		_RockAntiTiling("Rock Anti Tiling", Vector) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 5.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform float3 _AntiTilingVector;
		uniform sampler2D _AntiTilingTexture;
		uniform float3 _RockAntiTiling;
		uniform sampler2D _Rock;
		uniform float _Rock1tile;
		uniform float3 _GrassAntiTiling;
		uniform sampler2D _Grass1;
		uniform float _Grass1tile;
		uniform float _SlopeMin;
		uniform float _SlopeMax;
		uniform sampler2D _ArialTreeTexture;
		uniform float _ArialTreeTile;


		inline float3 ASESafeNormalize(float3 inVec)
		{
			float dp3 = max( 0.001f , dot( inVec , inVec ) );
			return inVec* rsqrt( dp3);
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float2 temp_cast_0 = (_RockAntiTiling.x).xx;
			float2 uv_TexCoord9_g8 = i.uv_texcoord * temp_cast_0;
			float2 temp_cast_1 = (_RockAntiTiling.y).xx;
			float2 uv_TexCoord11_g8 = i.uv_texcoord * temp_cast_1;
			float2 temp_cast_2 = (_RockAntiTiling.z).xx;
			float2 uv_TexCoord13_g8 = i.uv_texcoord * temp_cast_2;
			float3 temp_cast_3 = (( ( ( tex2D( _AntiTilingTexture, uv_TexCoord9_g8 ).g + 0.0 ) * ( tex2D( _AntiTilingTexture, uv_TexCoord11_g8 ).g + 0.0 ) ) * ( tex2D( _AntiTilingTexture, uv_TexCoord13_g8 ).g + 0.0 ) )).xxx;
			float3 lerpResult21_g8 = lerp( _AntiTilingVector , temp_cast_3 , float3( 1,1,1 ));
			float3 RockColorVariation157 = lerpResult21_g8;
			float2 temp_cast_5 = (_Rock1tile).xx;
			float2 uv_TexCoord88 = i.uv_texcoord * temp_cast_5;
			float2 temp_cast_6 = (_GrassAntiTiling.x).xx;
			float2 uv_TexCoord9_g5 = i.uv_texcoord * temp_cast_6;
			float2 temp_cast_7 = (_GrassAntiTiling.y).xx;
			float2 uv_TexCoord11_g5 = i.uv_texcoord * temp_cast_7;
			float2 temp_cast_8 = (_GrassAntiTiling.z).xx;
			float2 uv_TexCoord13_g5 = i.uv_texcoord * temp_cast_8;
			float3 temp_cast_9 = (( ( ( tex2D( _AntiTilingTexture, uv_TexCoord9_g5 ).g + 0.0 ) * ( tex2D( _AntiTilingTexture, uv_TexCoord11_g5 ).g + 0.0 ) ) * ( tex2D( _AntiTilingTexture, uv_TexCoord13_g5 ).g + 0.0 ) )).xxx;
			float3 lerpResult21_g5 = lerp( _AntiTilingVector , temp_cast_9 , float3( 1,1,1 ));
			float3 GrassColorVariation60 = lerpResult21_g5;
			float2 temp_cast_11 = (_Grass1tile).xx;
			float2 uv_TexCoord54 = i.uv_texcoord * temp_cast_11;
			float4 color92 = IsGammaSpace() ? float4(0.4138842,1,0,1) : float4(0.1428077,1,0,1);
			float3 temp_output_7_0_g6 = float3(0,1,0);
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldTangent = WorldNormalVector( i, float3( 1, 0, 0 ) );
			float3 ase_worldBitangent = WorldNormalVector( i, float3( 0, 1, 0 ) );
			float3x3 ase_tangentToWorldFast = float3x3(ase_worldTangent.x,ase_worldBitangent.x,ase_worldNormal.x,ase_worldTangent.y,ase_worldBitangent.y,ase_worldNormal.y,ase_worldTangent.z,ase_worldBitangent.z,ase_worldNormal.z);
			float3 tangentToWorldDir1_g6 = ASESafeNormalize( mul( ase_tangentToWorldFast, temp_output_7_0_g6 ) );
			float dotResult2_g6 = dot( tangentToWorldDir1_g6 , temp_output_7_0_g6 );
			float lerpResult4_g6 = lerp( _SlopeMin , _SlopeMax , abs( dotResult2_g6 ));
			float Slope82 = saturate( lerpResult4_g6 );
			float4 lerpResult86 = lerp( ( float4( RockColorVariation157 , 0.0 ) * tex2D( _Rock, uv_TexCoord88 ) ) , ( ( float4( GrassColorVariation60 , 0.0 ) * tex2D( _Grass1, uv_TexCoord54 ) ) * color92 ) , Slope82);
			float2 temp_cast_12 = (_ArialTreeTile).xx;
			float2 uv_TexCoord103 = i.uv_texcoord * temp_cast_12;
			float4 tex2DNode102 = tex2D( _ArialTreeTexture, uv_TexCoord103 );
			float4 lerpResult113 = lerp( lerpResult86 , tex2DNode102 , float4( 0,0,0,0 ));
			o.Albedo = lerpResult113.rgb;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
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
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
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
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
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
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18933
492;73;2475;1010;2467.154;1757.028;1.3;True;True
Node;AmplifyShaderEditor.CommentaryNode;61;-942,-1024;Inherit;False;832;339;Grass Anti Tiling;3;154;60;129;Color Variation;0.0003061295,0,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;155;-944,-1424;Inherit;False;832;339;Grass Anti Tiling;3;158;157;156;Color Variation;0.0003061295,0,1,1;0;0
Node;AmplifyShaderEditor.Vector3Node;154;-889.8752,-945.6458;Inherit;False;Property;_GrassAntiTiling;Grass Anti Tiling;16;0;Create;True;0;0;0;False;0;False;0,0,0;0.002,0.02,0.2;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;55;-1178,172.1;Inherit;False;Property;_Grass1tile;Grass 1 tile;0;0;Create;True;0;0;0;False;0;False;2;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;83;-80,-1024;Inherit;False;685.0647;397.9999;Calculates Slope;5;80;79;73;81;82;Slope;1,0,0,1;0;0
Node;AmplifyShaderEditor.Vector3Node;158;-880,-1346;Inherit;False;Property;_RockAntiTiling;Rock Anti Tiling;18;0;Create;True;0;0;0;False;0;False;0,0,0;0.002,0.02,0.2;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;129;-640,-928;Inherit;False;Anti Tiling;12;;5;7a9ec6dda0cb2dd47af47567be7687ed;0;3;8;FLOAT;0.002;False;10;FLOAT;0.02;False;12;FLOAT;0.2;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;60;-336,-896;Inherit;False;GrassColorVariation;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;156;-640,-1330.3;Inherit;False;Anti Tiling;12;;8;7a9ec6dda0cb2dd47af47567be7687ed;0;3;8;FLOAT;0.002;False;10;FLOAT;0.02;False;12;FLOAT;0.2;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;54;-1034,156.1;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector3Node;73;-32,-816;Inherit;False;Constant;_Vector1;Vector 1;11;0;Create;True;0;0;0;False;0;False;0,1,0;0,1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;79;-32,-976;Inherit;False;Property;_SlopeMin;Slope Min;2;0;Create;True;0;0;0;False;0;False;-1;2.12;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;87;-1147.6,572.1002;Inherit;False;Property;_Rock1tile;Rock 1 tile;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-32,-896;Inherit;False;Property;_SlopeMax;Slope Max;1;0;Create;True;0;0;0;False;0;False;1;-1.24;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;157;-336,-1298;Inherit;False;RockColorVariation;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;63;-697.9999,-35.9;Inherit;False;60;GrassColorVariation;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;81;128,-896;Inherit;False;Slope;-1;;6;55485e5d40c5f8f41983cb93729de4e0;0;3;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;88;-1003.6,572.1002;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;51;-793.9999,44.1;Inherit;True;Property;_Grass1;Grass 1;5;0;Create;True;0;0;0;False;0;False;-1;7b4b44557efd3ef4190530e2e45001bf;7b4b44557efd3ef4190530e2e45001bf;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;89;-779.5999,572.1002;Inherit;True;Property;_Rock;Rock;4;0;Create;True;0;0;0;False;0;False;-1;da8cece31ebe275419c6fe089590f85b;da8cece31ebe275419c6fe089590f85b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;92;-422.7468,243.7077;Inherit;False;Constant;_Color0;Color 0;13;0;Create;True;0;0;0;False;0;False;0.4138842,1,0,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;91;-683.5999,492.1001;Inherit;False;157;RockColorVariation;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;104;-914.6414,845.9297;Inherit;False;Property;_ArialTreeTile;Arial Tree Tile;10;0;Create;True;0;0;0;False;0;False;0;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-352.9998,56.1;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;368,-896;Inherit;False;Slope;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;-33.86827,498.4038;Inherit;False;82;Slope;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;-121.0663,204.3357;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;103;-717.6412,821.9297;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;90;-475.5999,572.1002;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;107;1520,-1024;Inherit;False;877.9;354.1;Altitude Mask;4;106;105;134;135;;1,0.5962105,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;101;624,-1024;Inherit;False;869.7001;338.8;Height map;4;132;133;100;99;;0,1,0.1748705,1;0;0
Node;AmplifyShaderEditor.LerpOp;86;170.1999,358.7;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;102;-446.5637,811.3072;Inherit;True;Property;_ArialTreeTexture;Arial Tree Texture;9;0;Create;True;0;0;0;False;0;False;-1;None;dcf272e1bc54fbc40b28241c4560048a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;134;1536,-897;Inherit;False;Property;_AltitudeMapTile;Altitude Map Tile;17;0;Create;True;0;0;0;False;0;False;1;0.0001;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;-1098.001,284.1;Inherit;False;Property;_GrassNormalScale;Grass Normal Scale;6;0;Create;True;0;0;0;False;0;False;10;10;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;105;1920,-897;Inherit;True;Property;_AltitudeMap;AltitudeMap;11;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;52;-793.9999,236.1;Inherit;True;Property;_Grass1normal;Grass 1 normal;7;0;Create;True;0;0;0;False;0;False;-1;52979744929b5af42a3f726bd4dc72b2;52979744929b5af42a3f726bd4dc72b2;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;113;500.1147,611.9128;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;100;1280,-896;Inherit;False;HeightMap;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;153;169.2984,862.0663;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;99;992,-896;Inherit;True;Property;_HeightMap;HeightMap;8;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;133;657.3,-897.3;Inherit;False;Property;_HeightMapTile;Height Map Tile;15;0;Create;True;0;0;0;False;0;False;1;0.0001;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;135;1712,-897;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;132;784,-896;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;106;2208,-897;Inherit;False;AltitudeMask;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;971.6459,476.17;Float;False;True;-1;7;ASEMaterialInspector;0;0;Standard;TestShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;129;8;154;1
WireConnection;129;10;154;2
WireConnection;129;12;154;3
WireConnection;60;0;129;0
WireConnection;156;8;158;1
WireConnection;156;10;158;2
WireConnection;156;12;158;3
WireConnection;54;0;55;0
WireConnection;157;0;156;0
WireConnection;81;5;79;0
WireConnection;81;6;80;0
WireConnection;81;7;73;0
WireConnection;88;0;87;0
WireConnection;51;1;54;0
WireConnection;89;1;88;0
WireConnection;62;0;63;0
WireConnection;62;1;51;0
WireConnection;82;0;81;0
WireConnection;131;0;62;0
WireConnection;131;1;92;0
WireConnection;103;0;104;0
WireConnection;90;0;91;0
WireConnection;90;1;89;0
WireConnection;86;0;90;0
WireConnection;86;1;131;0
WireConnection;86;2;85;0
WireConnection;102;1;103;0
WireConnection;105;1;135;0
WireConnection;52;1;54;0
WireConnection;52;5;53;0
WireConnection;113;0;86;0
WireConnection;113;1;102;0
WireConnection;100;0;99;0
WireConnection;153;0;102;0
WireConnection;99;1;132;0
WireConnection;135;0;134;0
WireConnection;132;0;133;0
WireConnection;106;0;105;0
WireConnection;0;0;113;0
ASEEND*/
//CHKSM=F14F0065CB29497A5AC3E3058E3927A3B43A588C
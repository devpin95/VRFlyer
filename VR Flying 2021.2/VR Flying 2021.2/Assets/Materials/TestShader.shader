// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TestShader"
{
	Properties
	{
		_Grass1("Grass 1", 2D) = "white" {}
		_Rock("Rock", 2D) = "white" {}
		_AntiTilingTexture("Anti Tiling Texture", 2D) = "white" {}
		_Grass1normal("Grass 1 normal", 2D) = "white" {}
		_GrassNormalScale("Grass Normal Scale", Range( 0 , 10)) = 10
		_Grass1tile("Grass 1 tile", Float) = 500
		_Rock1tile("Rock 1 tile", Float) = 500
		_AntiTiling1("Anti Tiling 1", Float) = 0.002
		_AntiTiling2("Anti Tiling 2", Float) = 0.02
		_AntiTiling3("Anti Tiling 3", Float) = 0.2
		_Vector1("Vector 1", Vector) = (0,1,0,0)
		_SlopeMin("Slope Min", Float) = -1
		_SlopeMax("Slope Max", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
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

		uniform sampler2D _Grass1normal;
		uniform float _Grass1tile;
		uniform float _GrassNormalScale;
		uniform sampler2D _AntiTilingTexture;
		uniform float _AntiTiling1;
		uniform float _AntiTiling2;
		uniform float _AntiTiling3;
		uniform sampler2D _Rock;
		uniform float _Rock1tile;
		uniform sampler2D _Grass1;
		uniform float _SlopeMin;
		uniform float _SlopeMax;
		uniform float3 _Vector1;


		inline float3 ASESafeNormalize(float3 inVec)
		{
			float dp3 = max( 0.001f , dot( inVec , inVec ) );
			return inVec* rsqrt( dp3);
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 temp_cast_0 = (_Grass1tile).xx;
			float2 uv_TexCoord54 = i.uv_texcoord * temp_cast_0;
			o.Normal = UnpackScaleNormal( tex2D( _Grass1normal, uv_TexCoord54 ), _GrassNormalScale );
			float2 temp_cast_1 = (_AntiTiling1).xx;
			float2 uv_TexCoord9_g1 = i.uv_texcoord * temp_cast_1;
			float2 temp_cast_2 = (_AntiTiling2).xx;
			float2 uv_TexCoord11_g1 = i.uv_texcoord * temp_cast_2;
			float2 temp_cast_3 = (_AntiTiling3).xx;
			float2 uv_TexCoord13_g1 = i.uv_texcoord * temp_cast_3;
			float3 temp_cast_4 = (( ( ( tex2D( _AntiTilingTexture, uv_TexCoord9_g1 ).g + 0.0 ) * ( tex2D( _AntiTilingTexture, uv_TexCoord11_g1 ).g + 0.0 ) ) * ( tex2D( _AntiTilingTexture, uv_TexCoord13_g1 ).g + 0.0 ) )).xxx;
			float3 lerpResult21_g1 = lerp( float3(0.9,0.9,0.9) , temp_cast_4 , float3( 1,1,1 ));
			float3 ColorVariation60 = lerpResult21_g1;
			float2 temp_cast_6 = (_Rock1tile).xx;
			float2 uv_TexCoord88 = i.uv_texcoord * temp_cast_6;
			float3 temp_output_7_0_g2 = _Vector1;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldTangent = WorldNormalVector( i, float3( 1, 0, 0 ) );
			float3 ase_worldBitangent = WorldNormalVector( i, float3( 0, 1, 0 ) );
			float3x3 ase_tangentToWorldFast = float3x3(ase_worldTangent.x,ase_worldBitangent.x,ase_worldNormal.x,ase_worldTangent.y,ase_worldBitangent.y,ase_worldNormal.y,ase_worldTangent.z,ase_worldBitangent.z,ase_worldNormal.z);
			float3 tangentToWorldDir1_g2 = ASESafeNormalize( mul( ase_tangentToWorldFast, temp_output_7_0_g2 ) );
			float dotResult2_g2 = dot( tangentToWorldDir1_g2 , temp_output_7_0_g2 );
			float lerpResult4_g2 = lerp( _SlopeMin , _SlopeMax , abs( dotResult2_g2 ));
			float Slope82 = saturate( lerpResult4_g2 );
			float4 lerpResult86 = lerp( ( float4( ColorVariation60 , 0.0 ) * tex2D( _Rock, uv_TexCoord88 ) ) , ( float4( ColorVariation60 , 0.0 ) * tex2D( _Grass1, uv_TexCoord54 ) ) , Slope82);
			o.Albedo = lerpResult86.rgb;
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
320;73;2647;885;1241.966;137.3497;1;True;True
Node;AmplifyShaderEditor.CommentaryNode;61;-896,-1024;Inherit;False;802;351;Anti Tiling;5;57;60;58;59;71;Color Variation;0.0003061295,0,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;59;-848,-784;Inherit;False;Property;_AntiTiling3;Anti Tiling 3;10;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;58;-848,-880;Inherit;False;Property;_AntiTiling2;Anti Tiling 2;9;0;Create;True;0;0;0;False;0;False;0.02;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;57;-848,-960;Inherit;False;Property;_AntiTiling1;Anti Tiling 1;8;0;Create;True;0;0;0;False;0;False;0.002;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;83;-80,-1024;Inherit;False;685.0647;397.9999;Calculates Slope;5;80;79;73;81;82;Slope;1,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-944,176;Inherit;False;Property;_Grass1tile;Grass 1 tile;6;0;Create;True;0;0;0;False;0;False;500;500;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;71;-624,-928;Inherit;False;Anti Tiling;2;;1;7a9ec6dda0cb2dd47af47567be7687ed;0;3;8;FLOAT;0.002;False;10;FLOAT;0.02;False;12;FLOAT;0.2;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;87;-913.5999,576.0001;Inherit;False;Property;_Rock1tile;Rock 1 tile;7;0;Create;True;0;0;0;False;0;False;500;500;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;73;-32,-816;Inherit;False;Property;_Vector1;Vector 1;11;0;Create;True;0;0;0;False;0;False;0,1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;80;-32,-896;Inherit;False;Property;_SlopeMax;Slope Max;13;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;79;-32,-976;Inherit;False;Property;_SlopeMin;Slope Min;12;0;Create;True;0;0;0;False;0;False;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;81;128,-896;Inherit;False;Slope;-1;;2;55485e5d40c5f8f41983cb93729de4e0;0;3;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;54;-800,160;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;88;-769.5999,576.0001;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;60;-320,-896;Inherit;False;ColorVariation;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;368,-896;Inherit;False;Slope;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;63;-464,-32;Inherit;False;60;ColorVariation;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;89;-545.6,576.0001;Inherit;True;Property;_Rock;Rock;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;51;-560,48;Inherit;True;Property;_Grass1;Grass 1;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;91;-449.5999,496.0001;Inherit;False;60;ColorVariation;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;53;-864,288;Inherit;False;Property;_GrassNormalScale;Grass Normal Scale;5;0;Create;True;0;0;0;False;0;False;10;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;-179,56;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;90;-241.6,576.0001;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;104.1318,467.1036;Inherit;False;82;Slope;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;52;-560,240;Inherit;True;Property;_Grass1normal;Grass 1 normal;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;86;294.5,296.4999;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;623.7664,41.40806;Float;False;True;-1;7;ASEMaterialInspector;0;0;Standard;TestShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;71;8;57;0
WireConnection;71;10;58;0
WireConnection;71;12;59;0
WireConnection;81;5;79;0
WireConnection;81;6;80;0
WireConnection;81;7;73;0
WireConnection;54;0;55;0
WireConnection;88;0;87;0
WireConnection;60;0;71;0
WireConnection;82;0;81;0
WireConnection;89;1;88;0
WireConnection;51;1;54;0
WireConnection;62;0;63;0
WireConnection;62;1;51;0
WireConnection;90;0;91;0
WireConnection;90;1;89;0
WireConnection;52;1;54;0
WireConnection;52;5;53;0
WireConnection;86;0;90;0
WireConnection;86;1;62;0
WireConnection;86;2;85;0
WireConnection;0;0;86;0
WireConnection;0;1;52;0
ASEEND*/
//CHKSM=ED8299C6EBF27C593EA41929D8628F92BE1C0ABC
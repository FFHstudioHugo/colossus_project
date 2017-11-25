// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using System;


namespace AmplifyShaderEditor
{
	public class ASETextureArrayCreator : EditorWindow
	{
		[MenuItem( "Window/Amplify Shader Editor/Texture Array Creator", false, 1001 )]
		static void ShowWindow()
		{
			ASETextureArrayCreator window = EditorWindow.GetWindow<ASETextureArrayCreator>();
			window.titleContent.text = "Texture Array";
			window.minSize = new Vector2( 302, 350 );
			window.Show();
		}

		[SerializeField]
		private List<Texture2D> m_allTextures;

		[SerializeField]
		private ReorderableList m_listTextures = null;

		[SerializeField]
		private bool m_linearMode = false;

		[SerializeField]
		private string m_folderPath = "Assets/";

		[SerializeField]
		private string m_fileName = "NewTextureArray";

		[SerializeField]
		private TextureWrapMode m_wrapMode = TextureWrapMode.Repeat;

		[SerializeField]
		private FilterMode m_filterMode = FilterMode.Bilinear;

		[SerializeField]
		private int m_anisoLevel = 1;

		[SerializeField]
		private int m_previewSize = 16;

		[SerializeField]
		private bool m_mipMaps = true;

		[SerializeField]
		private int m_selectedSizeX = 4;

		[SerializeField]
		private int m_selectedSizeY = 4;

		[SerializeField]
		private int m_selectedFormat = 1;

		private int[] m_sizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
		private string[] m_sizesStr = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };

		private TextureFormat[] m_formats = { TextureFormat.ARGB32, TextureFormat.RGBA32, TextureFormat.RGB24, TextureFormat.Alpha8 };
		private string[] m_formatsStr = { "ARGB32", "RGBA32", "RGB24", "Alpha8" };

		private GUIStyle m_contentStyle = null;
		private GUIStyle m_pathButtonStyle = null;
		private GUIContent m_pathButtonContent = new GUIContent();

		private Vector2 m_scrollPos;
		private Texture2DArray m_lastSaved;
		private bool m_lockRatio = true;
		private string m_message = string.Empty;

		private void OnEnable()
		{
			if( m_contentStyle == null )
			{
				m_contentStyle = new GUIStyle( GUIStyle.none );
				m_contentStyle.margin = new RectOffset( 6, 4, 5, 5 );
			}

			if( m_pathButtonStyle == null )
			{
				m_pathButtonStyle = new GUIStyle( "minibutton" );
			}

			if( m_allTextures == null )
				m_allTextures = new List<Texture2D>();

			if( m_listTextures == null )
			{
				m_listTextures = new ReorderableList( m_allTextures, typeof( Texture2D ), true, true, true, true );
				m_listTextures.elementHeight = 16;

				m_listTextures.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
				{
					m_allTextures[ index ] = (Texture2D)EditorGUI.ObjectField( rect, "Texture " + index, m_allTextures[ index ], typeof( Texture2D ), false );
				};

				m_listTextures.drawHeaderCallback = ( Rect rect ) =>
				{
					m_previewSize = EditorGUI.IntSlider( rect, "Texture List", m_previewSize, 16, 64 );
					if( (float)m_previewSize != m_listTextures.elementHeight )
						m_listTextures.elementHeight = m_previewSize;
				};
				m_listTextures.onAddCallback = ( list ) =>
				{
					m_allTextures.Add( null );
				};

				m_listTextures.onRemoveCallback = ( list ) =>
				{
					m_allTextures.RemoveAt( list.index );
				};
			}
		}

		private void OnDestroy()
		{
			if( m_allTextures != null )
			{
				m_allTextures.Clear();
				m_allTextures = null;
			}
		}

		void OnGUI()
		{
			m_scrollPos = EditorGUILayout.BeginScrollView( m_scrollPos, GUILayout.Height( Screen.height ) );
			float cachedWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;
			EditorGUILayout.BeginVertical( m_contentStyle );

			// build button
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup( m_allTextures.Count <= 0 );
			if( GUILayout.Button( "Build Array", "prebutton", GUILayout.Height( 20 ) ) )
			{
				bool showWarning = false;
				for( int i = 0; i < m_allTextures.Count; i++ )
				{
					if( m_allTextures[ i ].width != m_sizes[ m_selectedSizeX ] || m_allTextures[ i ].height != m_sizes[ m_selectedSizeY ] )
					{
						showWarning = true;
					}
				}

				if( !showWarning )
				{
					m_message = string.Empty;
					BuildArray();
				}
				else if( EditorUtility.DisplayDialog( "Warning!", "Some textures need to be resized to fit the selected size. Do you want to continue?", "Yes", "No" ) )
				{
					m_message = string.Empty;
					BuildArray();
				}
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup( m_lastSaved == null );
			GUIContent icon = EditorGUIUtility.IconContent( "icons/d_ViewToolZoom.png" );
			if( GUILayout.Button( icon, "prebutton", GUILayout.Width( 28 ), GUILayout.Height( 20 ) ) )
			{
				EditorGUIUtility.PingObject( m_lastSaved );
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			// message
			if( !string.IsNullOrEmpty( m_message ) )
				if( GUILayout.Button( "BUILD REPORT (click to hide):\n\n" + m_message, "helpbox" ) )
					m_message = string.Empty;

			// options
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel( "Size" );
			EditorGUIUtility.labelWidth = 16;
			m_selectedSizeX = EditorGUILayout.Popup( "X", m_selectedSizeX, m_sizesStr );
			EditorGUI.BeginDisabledGroup( m_lockRatio );
			m_selectedSizeY = EditorGUILayout.Popup( "Y", m_lockRatio ? m_selectedSizeX : m_selectedSizeY, m_sizesStr );
			EditorGUI.EndDisabledGroup();
			EditorGUIUtility.labelWidth = 100;
			m_lockRatio = GUILayout.Toggle( m_lockRatio, "L", "minibutton", GUILayout.Width( 18 ) );
			EditorGUILayout.EndHorizontal();
			m_linearMode = EditorGUILayout.Toggle( "Linear", m_linearMode );
			m_mipMaps = EditorGUILayout.Toggle( "Mip Maps", m_mipMaps );
			m_wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup( "Wrap Mode", m_wrapMode );
			m_filterMode = (FilterMode)EditorGUILayout.EnumPopup( "Filter Mode", m_filterMode );
			m_anisoLevel = EditorGUILayout.IntSlider( "Aniso Level", m_anisoLevel, 0, 16 );
			m_selectedFormat = EditorGUILayout.Popup( "Format", m_selectedFormat, m_formatsStr );
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField( "Path and Name" );
			EditorGUILayout.BeginHorizontal();
			m_pathButtonContent.text = m_folderPath;
			Vector2 buttonSize = m_pathButtonStyle.CalcSize( m_pathButtonContent );
			if( GUILayout.Button( m_pathButtonContent, m_pathButtonStyle, GUILayout.MaxWidth( Mathf.Min( Screen.width * 0.5f, buttonSize.x ) ) ) )
			{
				string folderpath = EditorUtility.OpenFolderPanel( "Save Texture Array to folder", "Assets/", "" );
				folderpath = FileUtil.GetProjectRelativePath( folderpath );
				if( string.IsNullOrEmpty( folderpath ) )
					m_folderPath = "Assets/";
				else
					m_folderPath = folderpath + "/";
			}
			m_fileName = EditorGUILayout.TextField( m_fileName, GUILayout.ExpandWidth( true ) );
			EditorGUILayout.LabelField( ".asset", GUILayout.MaxWidth( 40 ) );
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();

			// list
			EditorGUILayout.Separator();
			if( m_listTextures != null )
				m_listTextures.DoLayoutList();

			GUILayout.Space( 20 );
			EditorGUILayout.EndVertical();
			EditorGUIUtility.labelWidth = cachedWidth;
			EditorGUILayout.EndScrollView();
		}

		private void BuildArray()
		{
			int sizeX = m_sizes[ m_selectedSizeX ];
			int sizeY = m_sizes[ m_selectedSizeY ];

			Texture2DArray textureArray = new Texture2DArray( sizeX, sizeY, m_allTextures.Count, m_formats[ m_selectedFormat ], m_mipMaps, m_linearMode );

			for( int i = 0; i < m_allTextures.Count; i++ )
			{
				// build report
				int widthChanges = m_allTextures[ i ].width < sizeX ? -1 : m_allTextures[ i ].width > sizeX ? 1 : 0;
				int heightChanges = m_allTextures[ i ].height < sizeY ? -1 : m_allTextures[ i ].height > sizeY ? 1 : 0;
				if( ( widthChanges < 0 && heightChanges <= 0 ) || ( widthChanges <= 0 && heightChanges < 0 ) )
					m_message += m_allTextures[ i ].name + " was upscaled\n";
				else if( ( widthChanges > 0 && heightChanges >= 0 ) || ( widthChanges >= 0 && heightChanges > 0 ) )
					m_message += m_allTextures[ i ].name + " was downscaled\n";
				else if( ( widthChanges > 0 && heightChanges < 0 ) || ( widthChanges < 0 && heightChanges > 0 ) )
					m_message += m_allTextures[ i ].name + " changed dimensions\n";

				// blit image to upscale or downscale the image to any size
				RenderTexture rt = RenderTexture.GetTemporary( sizeX, sizeY, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 8 );
				RenderTexture cache = RenderTexture.active;
				RenderTexture.active = rt;
				bool cachedsrgb = GL.sRGBWrite;
				GL.sRGBWrite = true;
				Graphics.Blit( m_allTextures[ i ], rt );
				GL.sRGBWrite = cachedsrgb;
				Texture2D t2d = new Texture2D( sizeX, sizeY );
				t2d.ReadPixels( new Rect( 0, 0, sizeX, sizeY ), 0, 0 );
				RenderTexture.active = cache;
				RenderTexture.ReleaseTemporary( rt );
				textureArray.SetPixels( t2d.GetPixels( 0 ), i, 0 );
			}

			if( m_message.Length > 0 )
				m_message = m_message.Substring( 0, m_message.Length - 1 );

			textureArray.wrapMode = m_wrapMode;
			textureArray.filterMode = m_filterMode;
			textureArray.anisoLevel = m_anisoLevel;
			textureArray.Apply();

			string path = m_folderPath + m_fileName + ".asset";
			Texture2DArray outfile = AssetDatabase.LoadMainAssetAtPath( path ) as Texture2DArray;
			if( outfile != null )
			{
				EditorUtility.CopySerialized( textureArray, outfile );
				AssetDatabase.SaveAssets();
				EditorGUIUtility.PingObject( outfile );
				m_lastSaved = outfile;
			}
			else
			{
				AssetDatabase.CreateAsset( textureArray, path );
				EditorGUIUtility.PingObject( textureArray );
				m_lastSaved = textureArray;
			}
		}
	}
}

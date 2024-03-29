using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Toggle Switch", "Logical Operators", "Switch between any of its input ports" )]
	public class ToggleSwitchNode : PropertyNode
	{
		private const string InputPortName = "In ";
		private const string CurrSelectedStr = "Toogle Value";
		private const string LerpOp = "lerp({0},{1},{2})";

		[SerializeField]
		private string[] AvailableInputsLabels = { "0", "1" };

		[SerializeField]
		private int[] AvailableInputsValues = { 0, 1 };

		[SerializeField]
		private int m_currentSelectedInput = 0;

		[SerializeField]
		private WirePortDataType m_mainDataType = WirePortDataType.FLOAT;

		private int m_cachedPropertyId = -1;

		private Rect m_varRect;
		private bool m_editing;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( m_mainDataType, false, InputPortName + "0" );
			AddInputPort( m_mainDataType, false, InputPortName + "1" );

			AddOutputPort( m_mainDataType, m_inputPorts[ 0 ].Name );
			m_insideSize.Set( 50, 25 );
			m_currentParameterType = PropertyType.Property;
			m_customPrefix = "Toggle Switch";
			
			m_availableAttribs.Clear();
			m_availableAttribs.Add( new PropertyAttributes( "Toggle", "[Toggle]" ) );

			m_drawAttributes = false;
			m_freeType = false;
			m_useVarSubtitle = true;
			m_useInternalPortData = true;
			m_previewShaderGUID = "beeb138daeb592a4887454f81dba2b3f";
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			UIUtils.RegisterPropertyNode( this );
		}
		
		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if ( m_cachedPropertyId == -1 )
				m_cachedPropertyId = Shader.PropertyToID( "_Current" );

			PreviewMaterial.SetInt( m_cachedPropertyId, m_currentSelectedInput );
		}

		public override void OnConnectedOutputNodeChanges( int portId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( portId, otherNodeId, otherPortId, name, type );
			UpdateConnection();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnection();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			UpdateConnection();
		}

		void UpdateConnection()
		{
			WirePortDataType type1 = WirePortDataType.FLOAT;
			if( m_inputPorts[ 0 ].IsConnected )
				type1 = m_inputPorts[ 0 ].GetOutputConnection( 0 ).DataType;

			WirePortDataType type2 = WirePortDataType.FLOAT;
			if( m_inputPorts[ 1 ].IsConnected )
				type2 = m_inputPorts[ 1 ].GetOutputConnection( 0 ).DataType;

			m_mainDataType = UIUtils.GetPriority( type1 ) > UIUtils.GetPriority( type2 ) ? type1 : type2;

			m_inputPorts[ 0 ].ChangeType( m_mainDataType, false );
			m_inputPorts[ 1 ].ChangeType( m_mainDataType, false );

			m_outputPorts[ 0 ].ChangeProperties( m_currentSelectedInput == 0 ? m_inputPorts[ 0 ].Name : m_inputPorts[ 1 ].Name, m_mainDataType, false );
			//m_outputPorts[ 0 ].ChangeType( m_mainDataType, false );
		}

		public override void OnNodeLayout( DrawInfo drawInfo )
		{
			base.OnNodeLayout( drawInfo );

			m_varRect = m_remainingBox;
			m_varRect.width = 40 * drawInfo.InvertedZoom;
			m_varRect.height = 25 * drawInfo.InvertedZoom;
			m_varRect.x += m_remainingBox.width * 0.5f - m_varRect.width * 0.5f;
		}

		public override void DrawGUIControls( DrawInfo drawInfo )
		{
			base.DrawGUIControls( drawInfo );

			if ( drawInfo.CurrentEventType != EventType.MouseDown )
				return;

			if ( m_varRect.Contains( drawInfo.MousePosition ) )
			{
				m_editing = true;
			}
			else if ( m_editing )
			{
				m_editing = false;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			if( m_editing )
			{
				EditorGUI.BeginChangeCheck();
				m_currentSelectedInput = EditorGUIIntPopup( m_varRect, m_currentSelectedInput, AvailableInputsLabels, AvailableInputsValues, UIUtils.SwitchNodePopUp );
				if ( EditorGUI.EndChangeCheck() )
				{
					UpdateConnection();
					m_requireMaterialUpdate = true;
					m_editing = false;
				}
			}
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			base.OnNodeRepaint( drawInfo );

			if ( !m_isVisible )
				return;

			if ( !m_editing && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD4 )
				GUI.Label( m_varRect, AvailableInputsLabels[ m_currentSelectedInput ], UIUtils.SwitchNodePopUp );
		}

		public override void DrawMainPropertyBlock()
		{
			base.DrawMainPropertyBlock();
			EditorGUILayout.Separator();
			EditorGUI.BeginChangeCheck();
			m_currentSelectedInput = EditorGUILayoutIntPopup( CurrSelectedStr, m_currentSelectedInput, AvailableInputsLabels, AvailableInputsValues );
			if ( EditorGUI.EndChangeCheck() )
			{
				UpdateConnection();
				m_requireMaterialUpdate = true;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			m_precisionString = UIUtils.FinalPrecisionWirePortToCgType( m_currentPrecisionType, m_outputPorts[ 0 ].DataType );

			string resultA = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, ignoreLocalvar, true );
			string resultB = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, m_mainDataType, ignoreLocalvar, true );
			return string.Format( LerpOp, resultA, resultB, m_propertyName );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_currentSelectedInput = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentSelectedInput );
		}

		public override string GetPropertyValue()
		{
			return "[Toggle]" + m_propertyName + "(\"" + m_propertyInspectorName + "\", Float) = " + m_currentSelectedInput;
		}

		public override string GetUniformValue()
		{
			return string.Format( Constants.UniformDec, UIUtils.FinalPrecisionWirePortToCgType( m_currentPrecisionType, WirePortDataType.FLOAT ), m_propertyName );
		}

		public override bool GetUniformData( out string dataType, out string dataName )
		{
			dataType = UIUtils.FinalPrecisionWirePortToCgType( m_currentPrecisionType, WirePortDataType.FLOAT );
			dataName = m_propertyName;
			return true;
		}

		public override void UpdateMaterial( Material mat )
		{
			base.UpdateMaterial( mat );
			if ( UIUtils.IsProperty( m_currentParameterType ) && !InsideShaderFunction )
			{
				mat.SetFloat( m_propertyName, ( float ) m_currentSelectedInput );
			}
		}

		public override void SetMaterialMode( Material mat , bool fetchMaterialValues )
		{
			base.SetMaterialMode( mat , fetchMaterialValues );
			if ( fetchMaterialValues && m_materialMode && UIUtils.IsProperty( m_currentParameterType ) && mat.HasProperty( m_propertyName ) )
			{
				m_currentSelectedInput = ( int ) mat.GetFloat( m_propertyName );
			}
		}

		public override void ForceUpdateFromMaterial( Material material )
		{
			if ( UIUtils.IsProperty( m_currentParameterType ) && material.HasProperty( m_propertyName ) )
				m_currentSelectedInput = ( int ) material.GetFloat( m_propertyName );
		}

		public override string GetPropertyValStr()
		{
			return PropertyName;			//return m_currentSelectedInput.ToString();
		}
	}
}

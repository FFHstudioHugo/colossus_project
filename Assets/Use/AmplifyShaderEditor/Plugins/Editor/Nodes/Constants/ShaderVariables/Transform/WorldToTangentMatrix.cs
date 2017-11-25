using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "World To Tangent Matrix", "Matrix Transform", "World to tangent transform matrix")]
	public sealed class WorldToTangentMatrix : ParentNode
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT3x3, "Out" );
			//UIUtils.AddNormalDependentCount();
			m_drawPreview = false;
		}

		//public override void Destroy()
		//{
		//	ContainerGraph.RemoveNormalDependentCount();
		//	base.Destroy();
		//}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData , ref dataCollector );
			dataCollector.DirtyNormal = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if ( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldToTangentMatrix();

			if( dataCollector.IsFragmentCategory )
			{
				dataCollector.ForceNormal = true;

				dataCollector.AddToInput( UniqueId, UIUtils.GetInputDeclarationFromType( m_currentPrecisionType, AvailableSurfaceInputs.WORLD_NORMAL ), true );
				dataCollector.AddToInput( UniqueId, Constants.InternalData, false );
			}

			GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, m_currentPrecisionType );

			return GeneratorUtils.WorldToTangentStr;
		}
	}
}

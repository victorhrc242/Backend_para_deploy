using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
namespace treinando_modelo_v1
{
    public class PostSaida
    {
        public bool Label { get; set; }  // se é relevante ou não, por exemplo
    }
 

public class ModeloInput
    {
        [LoadColumn(0)] public float CurtidasEmComum { get; set; }
        [LoadColumn(1)] public float TagsEmComum { get; set; }
        [LoadColumn(2)] public float EhSeguidor { get; set; }
        [LoadColumn(3)] public float Recente { get; set; }
        [LoadColumn(4)] public float JaVisualizou { get; set; }
        [LoadColumn(5)] public float TempoVisualizacaoUsuario { get; set; }
        [LoadColumn(6)] public float TotalVisualizacoesPost { get; set; }

        // Esse campo é a "resposta" que seu modelo vai aprender a prever
        [LoadColumn(7), ColumnName("Label")]
        public bool Label { get; set; }
    }

}

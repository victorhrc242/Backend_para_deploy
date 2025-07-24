using Microsoft.ML;
using Microsoft.ML.Data;
namespace dbRede.Algoritimo
{
 

   
        public class PostEntrada
        {
            [LoadColumn(0)] public float CurtidasEmComum { get; set; }
            [LoadColumn(1)] public float TagsEmComum { get; set; }
            [LoadColumn(2)] public float EhSeguidor { get; set; }
            [LoadColumn(3)] public float Recente { get; set; }
            [LoadColumn(4)] public float JaVisualizou { get; set; }
            [LoadColumn(5)] public float TempoVisualizacaoUsuario { get; set; }
            [LoadColumn(6)] public float TotalVisualizacoesPost { get; set; }
        }

        public class PostSaida
        {
            [ColumnName("PredictedLabel")] public bool Predicao { get; set; }
            public float[] Score { get; set; }
        }

        public static class ModeloML
        {
            private static readonly Lazy<PredictionEngine<PostEntrada, PostSaida>> _engine = new(() =>
            {
                var mlContext = new MLContext();
                var caminhoModelo = Path.Combine(AppContext.BaseDirectory, "MLModel", "modelo_feed.zip");
                ITransformer modeloTreinado = mlContext.Model.Load(caminhoModelo, out _);
                return mlContext.Model.CreatePredictionEngine<PostEntrada, PostSaida>(modeloTreinado);
            });

            public static PostSaida Prever(PostEntrada entrada)
            {
                return _engine.Value.Predict(entrada);
            }
        }
    }


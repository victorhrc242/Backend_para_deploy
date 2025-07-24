using Microsoft.ML;
using Microsoft.ML.Data;
using treinando_modelo_v1;

var mlContext = new MLContext();

// Dados de treino
var dadosTreino = new List<(PostEntrada Entrada, bool Label)>
{
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=1, Recente=1, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=5 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=1, Recente=0, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=2 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=0, Recente=1, JaVisualizou=1, TempoVisualizacaoUsuario=3, TotalVisualizacoesPost=10 }, false),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=0, Recente=0, JaVisualizou=1, TempoVisualizacaoUsuario=2, TotalVisualizacoesPost=15 }, false),
    (new PostEntrada { CurtidasEmComum=1, TagsEmComum=1, EhSeguidor=0, Recente=1, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=3 }, true),
    (new PostEntrada { CurtidasEmComum=3, TagsEmComum=2, EhSeguidor=0, Recente=1, JaVisualizou=1, TempoVisualizacaoUsuario=5, TotalVisualizacoesPost=40 }, true),
    (new PostEntrada { CurtidasEmComum=2, TagsEmComum=1, EhSeguidor=1, Recente=1, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=12 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=3, EhSeguidor=0, Recente=0, JaVisualizou=1, TempoVisualizacaoUsuario=1, TotalVisualizacoesPost=30 }, false),
    (new PostEntrada { CurtidasEmComum=4, TagsEmComum=0, EhSeguidor=0, Recente=0, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=1 }, true),
    (new PostEntrada { CurtidasEmComum=1, TagsEmComum=0, EhSeguidor=0, Recente=0, JaVisualizou=1, TempoVisualizacaoUsuario=2, TotalVisualizacoesPost=8 }, false),
    (new PostEntrada { CurtidasEmComum=3, TagsEmComum=2, EhSeguidor=1, Recente=1, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=18 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=1, EhSeguidor=0, Recente=1, JaVisualizou=1, TempoVisualizacaoUsuario=4, TotalVisualizacoesPost=50 }, false),
    (new PostEntrada { CurtidasEmComum=1, TagsEmComum=0, EhSeguidor=1, Recente=0, JaVisualizou=1, TempoVisualizacaoUsuario=3, TotalVisualizacoesPost=15 }, false),
    (new PostEntrada { CurtidasEmComum=2, TagsEmComum=2, EhSeguidor=0, Recente=1, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=7 }, true),
    (new PostEntrada { CurtidasEmComum=1, TagsEmComum=3, EhSeguidor=1, Recente=0, JaVisualizou=1, TempoVisualizacaoUsuario=1, TotalVisualizacoesPost=25 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=1, Recente=1, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=4 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=1, Recente=1, JaVisualizou=0, TempoVisualizacaoUsuario=0, TotalVisualizacoesPost=3 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=1, Recente=1, JaVisualizou=1, TempoVisualizacaoUsuario=2, TotalVisualizacoesPost=6 }, true),
    (new PostEntrada { CurtidasEmComum=0, TagsEmComum=0, EhSeguidor=1, Recente=1, JaVisualizou=1, TempoVisualizacaoUsuario=5, TotalVisualizacoesPost=10 }, true),
    (new PostEntrada { CurtidasEmComum=3, TagsEmComum=2, EhSeguidor=1, Recente=1, JaVisualizou=1, TempoVisualizacaoUsuario=5, TotalVisualizacoesPost=35 }, true),
};

// Converter para IDataView
var treinoData = mlContext.Data.LoadFromEnumerable(dadosTreino.Select(d => new ModeloInput
{
    CurtidasEmComum = d.Entrada.CurtidasEmComum,
    TagsEmComum = d.Entrada.TagsEmComum,
    EhSeguidor = d.Entrada.EhSeguidor,
    Recente = d.Entrada.Recente,
    JaVisualizou = d.Entrada.JaVisualizou,
    TempoVisualizacaoUsuario = d.Entrada.TempoVisualizacaoUsuario,
    TotalVisualizacoesPost = d.Entrada.TotalVisualizacoesPost,
    Label = d.Label
}));

// Pipeline de treino
var pipeline = mlContext.Transforms.Concatenate("Features",
        nameof(PostEntrada.CurtidasEmComum),
        nameof(PostEntrada.TagsEmComum),
        nameof(PostEntrada.EhSeguidor),
        nameof(PostEntrada.Recente),
        nameof(PostEntrada.JaVisualizou),
        nameof(PostEntrada.TempoVisualizacaoUsuario),
        nameof(PostEntrada.TotalVisualizacoesPost))
    .Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression());

// Treinar modelo
var modeloTreinado = pipeline.Fit(treinoData);

// Salvar modelo
mlContext.Model.Save(modeloTreinado, treinoData.Schema, "modelo_feed.zip");

Console.WriteLine("Modelo treinado e salvo em modelo_feed.zip");

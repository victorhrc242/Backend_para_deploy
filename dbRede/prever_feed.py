from fastapi import FastAPI
from pydantic import BaseModel
import numpy as np
from joblib import load

app = FastAPI()
modelo = load('modelo_feed.joblib')

class PostEntrada(BaseModel):
    curtidas_em_comum: int
    tags_em_comum: int
    eh_seguidor: int
    recente: int
    ja_visualizou: int
    tempo_visualizacao_usuario: int
    total_visualizacoes_post: int

@app.post("/score")
def calcular_score(posts: list[PostEntrada]):
    entradas = np.array([[p.curtidas_em_comum, p.tags_em_comum, p.eh_seguidor,
                          p.recente, p.ja_visualizou, p.tempo_visualizacao_usuario,
                          p.total_visualizacoes_post] for p in posts])
    scores = modelo.predict_proba(entradas)[:, 1]
    return [{"postId": i, "score": float(score)} for i, score in enumerate(scores)]

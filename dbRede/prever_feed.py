import sys
import json
import numpy as np
from joblib import load

# Carrega o modelo previamente treinado
modelo = load('modelo_feed.joblib')

# Ler todo o JSON do stdin
entrada_json = sys.stdin.read()

# Converte o conteúdo lido (string JSON) em lista de objetos Python
posts = json.loads(entrada_json)

# Lista para armazenar os resultados com pontuação
resposta = []

# Calcula a probabilidade (score) para cada post
for post in posts:
    entrada = np.array([
        post["curtidas_em_comum"],
        post["tags_em_comum"],
        post["eh_seguidor"],
        post["recente"],
        post["ja_visualizou"],
        post["tempo_visualizacao_usuario"],
        post["total_visualizacoes_post"]
    ]).reshape(1, -1)

    score = float(modelo.predict_proba(entrada)[0][1])
    resposta.append({
        "postId": post["id"],
        "score": score
    })

# Imprime o resultado no formato JSON
print(json.dumps(resposta))

import numpy as np
from sklearn.neural_network import MLPClassifier
from joblib import dump

# Novo formato:
# [curtidas_em_comum, tags_em_comum, eh_seguidor, recente, ja_visualizou, tempo_visualizacao_usuario, total_visualizacoes_post]

X = np.array([
    [0, 0, 1, 1, 0, 0, 5],
    [0, 0, 1, 0, 0, 0, 2],
    [0, 0, 0, 1, 1, 3, 10],
    [0, 0, 0, 0, 1, 2, 15],
    [1, 1, 0, 1, 0, 0, 3],
    [3, 2, 0, 1, 1, 5, 40],
    [2, 1, 1, 1, 0, 0, 12],
    [0, 3, 0, 0, 1, 1, 30],
    [4, 0, 0, 0, 0, 0, 1],
    [1, 0, 0, 0, 1, 2, 8],
    [3, 2, 1, 1, 0, 0, 18],
    [0, 1, 0, 1, 1, 4, 50],
    [1, 0, 1, 0, 1, 3, 15],
    [2, 2, 0, 1, 0, 0, 7],
    [1, 3, 1, 0, 1, 1, 25],
    [0, 0, 1, 1, 0, 0, 4],
    [0, 0, 1, 1, 0, 0, 3],
    [0, 0, 1, 1, 1, 2, 6],
    [0, 0, 1, 1, 1, 5, 10]
])

y = [
    1, 1, 0, 0,
    1, 1, 1, 0,
    1, 0, 1, 0,
    0, 1, 1, 1,
    1, 1, 1
]

modelo = MLPClassifier(hidden_layer_sizes=(12,), max_iter=1000, random_state=42)
modelo.fit(X, y)
dump(modelo, 'modelo_feed.joblib')

print("✅ Novo modelo treinado com visualizações e salvo com sucesso!")

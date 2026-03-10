import openai
import time
import textwrap
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity

def getClient():
    client = openai.OpenAI(api_key="none", base_url="http://localhost:8033/", timeout=14400)
    return client

def getData():
    file = open("D:\\Ilya\\Programming\\rag\\src\\simple-rag\\dbset.txt", encoding="utf-8", errors="ignore")
    data = file.readlines()
    return "".join(data)

def askLLM(client, text):
    prompt = f"Пожалуйста, дай детальный ответ на следующий вопрос:\n{text}"
    try:
        response = client.chat.completions.create(
            model='does not matter',
            messages = [
                {
                    "role": "system",
                    "content": "Ты - эксперт-помощник по ФТ и ADR (архитектурным решениям) в системах работодателя"
                },
                {
                    "role": "assistant",
                    "content": "1. Ты можешь детально прочитать и ответить на вопрос пользователя."
                },
                {
                    "role": "user",
                    "content": prompt
                }
            ],
            temperature = 0.1
        )
        return response.choices[0].message.content.strip()
    except Exception as e:
        return str(e)
    
def printFormattedRespose(response):
    wrapper = textwrap.TextWrapper(width=160)
    wrappedText = wrapper.fill(text = response)
    separator = "------------\n"
    print("Response:")
    print(separator)
    print(wrappedText)
    print(separator)

def calculateCosineSimilarity(text1, text2):
    vectorizer = TfidfVectorizer(
        stop_words='english',
        use_idf=True,
        norm="l2",
        ngram_range=(1, 2),
        analyzer='word',
        sublinear_tf=True
    )
    tfidf = vectorizer.fit_transform([text1, text2])
    similarity = cosine_similarity(tfidf[0:1], tfidf[1:2])
    return similarity[0][0]

def findBestMatchKeywordSearch(query, dbRecords):
    bestScore = 0
    bestRecord = None
    queryKeywords = set(query.lower().split())
    
    for record in dbRecords:
        recordKeywords = set(record.lower().split())
        commotKeywords = queryKeywords.intersection(recordKeywords)
        currentScore = len(commotKeywords)
        if currentScore > bestScore:
            bestScore = currentScore
            bestRecord = record
    return bestScore, bestRecord

client = getClient()
data = getData()
start_time = time.time()
dataSeparator = "============================================================================="
query = "ФТ 132 - расскажи про неё"
dbRecords = data.split(dataSeparator)
bestScore, bestRecord = findBestMatchKeywordSearch(query, dbRecords)
score = calculateCosineSimilarity(query, bestRecord)
print(f"Лучший cosine similatiry score: {score}")
augmented_input = query + ": " + bestRecord
llmResponse = askLLM(client, augmented_input)
printFormattedRespose(llmResponse)

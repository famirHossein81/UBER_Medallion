import chromadb
from sentence_transformers import SentenceTransformer
import psycopg2


db_params = {
    "host": "localhost",
    "database": "postgres", 
    "user": "postgres",
    "password": "123"
}

chroma_client = chromadb.PersistentClient(path="./chroma_db")
collection = chroma_client.get_or_create_collection(name="cancellation_reasons")

model = SentenceTransformer('all-MiniLM-L6-v2')

def load_data():
    try:
        conn = psycopg2.connect(**db_params)
        cur = conn.cursor()
        
        print("⏳ Fetching data from SQL...")
        query = """
            SELECT booking_id, unified_cancellation_reason 
            FROM gold.cleaned_dataset 
            WHERE unified_cancellation_reason IS NOT NULL 
            AND unified_cancellation_reason != 'N/A'
            LIMIT 500; -- Limit for speed during testing
        """
        cur.execute(query)
        rows = cur.fetchall()
        
        ids = []
        documents = []
        metadatas = []
        
        print(f"📊 Processing {len(rows)} records...")
        
        for row in rows:
            bid = str(row[0])
            reason = row[1]
            
            ids.append(bid)
            documents.append(reason)
            metadatas.append({"booking_id": bid, "reason": reason})

        if documents:
            print("🧠 Generating Vectors (this may take a moment)...")
            embeddings = model.encode(documents).tolist()
            
            print("💾 Saving to ChromaDB...")
            collection.add(
                ids=ids,
                embeddings=embeddings,
                metadatas=metadatas,
                documents=documents
            )
            print("✅ Success! Vector Database is ready.")
        else:
            print("⚠️ No data found to index.")

        cur.close()
        conn.close()

    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    load_data()
# Eloi
## LLM (OLLama + Gemma3-4 + SQLite RAG)

- Eloi is a combination of Ollama customization and Google's Gemma3-4, a powerful open-source LLM that can be run locally. 
- This project leverages Eloi for generating responses based on the context retrieved from your documents and includes `eloi_Modelfile` for additional customization and fine-tuning.

- Starting the application will simultaneously start the `ollama` process as a sidecar, while checking for updates to the `eloi_Modelfile` to build on top of `Gemma3-4`. 
- The first time run takes some time, however subsequent startups will be quicker due to a hashing mechanism (e.g.: `eloi_Modelfile.hash`), so it doesn't rebuild the model if there are no changes.

- You can give Eloi documents (PDFs, URLs) to learn from. She'll ingest them to a local **SQLite-backed RAG** store and queries them with `Ollama` + `Gemma3-4` + your own customized `eloi_Modelfile`. Before giving you a response. 
- Customize the `eloi_Modelfile` to your liking *(more personable, professional, technical, etc.).* An example is included, you have to rename it once you clone the repository.

---

### Business Use Cases

- Upload **multiple PDFs at once**, add **URLs**, and (optionally) pull from **Google Drive / Google Docs** (scaffold/integration-ready). 
- You can teach Eloi with your own documents (e.g.: thousand page PDF documents on medcine, local laws, etc.) for your small business.

1. *Personal assistant that doesn't require the internet.*
2. **Chat**: pulls context from the information you've taught her before responding.
3. **Ingest**: PDFs / URLs / Google Docs → extract text → chunk → embed → store in SQLite  
4. **Retrieve**: semantic search over embeddings + metadata filters  
5. **Generate**: send the retrieved context to an Ollama model for grounded answers

![Preview](Preview.png)

---

## Features

- **Bulk PDF upload** (multi-select) with staging + “Ingest Selected”
- **PDF text extraction** via **UglyToad.PdfPig**
- **Local-first RAG**:
  - Chunking + embeddings
  - **SQLite** persistence (documents, chunks, vectors, metadata)
  - Fast retrieval for small/medium collections
- **Ollama** integration for:
  - Embeddings model (e.g., `nomic-embed-text`)
  - Chat/completions model (e.g., `llama3`, `qwen2.5`, etc.)
- **Radzen Blazor Server UI**:
  - Tabs for **Upload PDFs**, **URLs**, **Google Drive / Docs**
  - Data grid with status + actions (remove/clear)
- **SignalR** progress updates:
  - Real-time ingest status (“staged”, “processing”, “done”, “failed”)
- ~~Google Drive / Docs integration~~ *(deprioritized for now):*
  - Wiring points included to fetch file content and ingest it like PDFs/URLs

---

## Tech Stack

- **Ollama** (local LLM + embeddings)
- **Gemma3-4** (Google)
- **SQLite** (RAG local persistence)
- **UglyToad.PdfPig** (PDF text extraction)
- **SignalR** (WebSockets for the seamless chat experience)

---

## Getting Started

### Prerequisites
- **.NET SDK** (matching the project’s target framework)
- **Ollama** installed and running locally  
  - Your ollama installation will run side-by-side (e.g.: `http://localhost:11434`)
  - Install: https://ollama.com  
  - Start the service: `ollama serve`
  - Select `gemma3-4` from the list of models available, let the base model download to your local directory.
  - `ollama pull nomic-embed-text` afterwards, get the embedding model as well.
  - **Then you no longer have to use the official Ollama app.**
 
- You can open the Eloi app and the `ollama` process will run in the background automatically with the customized Gemma model pre-loaded.
- Embeddings model example: `ollama pull nomic-embed-text`
- Rename `EXAMPLE_eloi_Modelfile` to `eloi_Modelfile`. You'll probably change this often as you learn each other.

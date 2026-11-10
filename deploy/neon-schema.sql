BEGIN;
CREATE EXTENSION IF NOT EXISTS vector;
CREATE TABLE IF NOT EXISTS conhecimentos (
    id uuid PRIMARY KEY,
    titulo text NOT NULL,
    texto text NOT NULL,
    fonte text NOT NULL,
    indice_chunk integer NOT NULL,
    embedding vector(1536) NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_conhecimentos_embedding
    ON conhecimentos USING hnsw (embedding vector_cosine_ops);
COMMIT;

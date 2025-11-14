 # AI Approach

C#/.Net is used in BE so I decided to use ML.Net for this project. Microsoft support ML.Net library for C# projects so it was easy to build lightweight, offline-friendly machine-learning pipelines.


## 1. Duplicate / Similar Incident Detection

**Goal** We can easily detect similar incidents of one targeted incident

Here are the flow,

1. We pre-trained a model with some data (text_embedding_corpus.csv) to generate relevant vector info when we create new incidents.
2. By using that vector data, stored in TextVector column, we can now detect other incidents similar to the targeted incident. `TextEmbeddingService` transform the text description into vector.
3. `SimilarityService` compares the new vector to stored vectors (kept in PostgreSQL) via cosine similarity. High scores indicate potential duplicates.

## 2. Automatic Waste Category Prediction

**Goal:** auto-fill the category field when users omit it, keeping data consistent.

**How it works**

1. `WasteClassificationService` trains an ML.NET multiclass classifier (SDCA Maximum Entropy) on a curated list of labeled phrases for each category: recyclables, hazardous, organic, e‑waste, bulk, and illegal dumping.
2. At runtime, if a submitted incident lacks a category, we pass its description through the classifier and use the predicted label.


## Limitation

The two models ( classifier and similarity detector ) are very light-weight and just demonstration purpose. They are trained with small data (around 1000 records) so it won't provide very exact result to us in same case at the moment.


## Automated Insight

Statistical Automated Insights are displayed on dashboard too.



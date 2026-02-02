# Search Architecture Decision — Firestore + Dedicated Search Engine

## Decision (Locked)
- Primary database: **Firestore**
- Search engine: **Dedicated full-text search service**
- Source of truth: **Firestore**
- Search index: **Derived, eventually consistent**

---

## Responsibility Split

### Firestore
- Tip storage (CRUD)
- Categories
- Users & roles
- Favorites
- Admin operations

### Search Engine
- Full-text search:
  - Title
  - Description
- Filtering:
  - Category
  - Tags
- Sorting:
  - Relevance (default)

---

## Data Flow (MVP)

1. Admin creates/updates/deletes a tip.
2. Tip is persisted in Firestore.
3. Backend syncs the change to the search index.
4. User search requests hit the search engine.
5. Search engine returns matching tip IDs.
6. Backend fetches full tip data from Firestore.

---

## Search Index — Minimum Schema

Each indexed document contains:
- `tipId`
- `title`
- `description`
- `categoryId`
- `tags[]`
- `isActive`

(Steps are **not** indexed.)

---

## MVP Search Behavior

- Search scope: title + description
- Matching: full-text
- Ranking: relevance only
- Filters:
  - Category (AND)
  - Tags (logic to be decided)
- Pagination: required
- Empty result state: explicitly handled

---

## Decisions Required (Blocking)

- Tag filter logic:
  - OR (match any selected tag)
  - AND (match all selected tags)
- Search index update strategy:
  - Real-time
  - Near-real-time (seconds delay acceptable)

---

## Future-Proofing (Out of MVP)

- Natural language → search query (AI search)
- Synonyms and typo tolerance
- Popularity-based boosting
- Search analytics
- Advanced ranking strategies

---

## What Not to Do (Yet)

- Do not index steps
- Do not over-engineer ranking
- Do not add analytics
- Do not support complex boolean queries

---

## Next Steps (Choose One)

1. Define exact search UX behavior
2. Finalize Firestore data model
3. Define Firestore → search sync contract
4. Decide tag filter logic (AND vs OR)

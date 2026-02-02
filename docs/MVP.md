# MVP Plan — Daily Tips Application

---

## Problem Statement
People want quick, practical daily-life tips with minimal friction and optional persistence when they sign in.

---

## Goal
Allow users to search, read, and save tips, while validating usage patterns starting from zero content.

---

## Target Users
- Anonymous users
- Logged-in users
- Admin users

---

## In Scope (MVP)

### Content

- Tip
  - Title
  - Description
  - Steps (ordered list)
  - Category (single)
  - Tags (free-form, multiple)
  - YouTube link (optional, single)

- Category
  - Name
  - Admin-managed

---

### User Roles & Access

#### Anonymous User
- No authentication
- Favorites stored in local storage
- Can view and search tips
- Can filter and sort results
- Can save tips to a single favorites list

#### Logged-in User
- Authentication enabled
- Favorites stored in database
- Local favorites merged on login (deduplicated)
- Same capabilities as anonymous users

#### Admin User
- Authenticated with admin role
- Same capabilities as logged-in users
- Can create, edit, and delete tips
- Can manage categories

---

### User Actions

- View tips
- Search tips (server-side)
- Filter by category
- Filter by tags
- Sort search results
- Save tips to a single favorites list

(No likes or reactions.)

---

### Pages

- Home page (search entry point)
- Search results page
- Tip detail page
- Category list page
- Admin tips management page (CRUD)

---

### Admin Functionality

- Create tips
- Edit tips
- Delete tips
- Assign categories
- Assign tags
- Add YouTube link
- Immediate publish (no drafts)

---

## Non-Functional Requirements

- SEO:
  - Indexable pages for tips and categories
  - Target Lighthouse SEO score: 100%

- Performance:
  - Target Lighthouse performance score: ≥90%

- Accessibility:
  - WCAG-compliant
  - Keyboard navigation
  - Screen reader support

- UI/UX:
  - Simple navigation
  - Responsive layout

---

## Out of Scope

- AI / natural language search
- Multiple favorite lists
- Likes or reactions
- Feedback system
- Analytics or reporting
- Media uploads
- Drafts, scheduling, or versioning
- Personalization
- Sharing features
- Localization
- Monetization

---

## Assumptions

- MVP launches with zero tips
- Search is fully server-side
- Categories are admin-managed
- Tags are free-form
- One favorites list per user
- Favorites merge is additive and deduplicated
- Multiple admins supported

---

## Risks

- Search quality depends on external search engine configuration
- Empty-state UX is critical at launch
- Free-form tags may create inconsistencies

---

## Success Criteria (MVP)

- Application behaves correctly with zero data
- Search returns relevant results
- Favorites persist correctly across login
- Admin can manage content end-to-end
- Lighthouse targets met

---

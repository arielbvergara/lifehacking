# Bugfix Requirements Document

## Introduction

The cache invalidation system is not properly invalidating cached data when entities are modified, resulting in stale statistics and data being served to users. The dashboard cache never gets invalidated when users, categories, or tips change, and individual category caches are not invalidated when tip counts change. This causes the API to return outdated information until the cache naturally expires (up to 1 day for some endpoints).

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN categories are created, updated, or deleted THEN the system does not invalidate the AdminDashboard cache, causing stale category statistics to be displayed

1.2 WHEN tips are created, updated, or deleted THEN the system does not invalidate individual category caches (Category_{guid}), causing stale tip counts to be displayed

1.3 WHEN tips are created, updated, or deleted in a category THEN the system does not invalidate the GetCategoryById cached response, causing stale tip counts to persist for up to 1 day

1.4 WHEN users are created or deleted THEN the system does not invalidate the AdminDashboard cache, causing stale user statistics to be displayed

1.5 WHEN tips are created or deleted THEN the system does not invalidate the AdminDashboard cache, causing stale tip statistics to be displayed

### Expected Behavior (Correct)

2.1 WHEN categories are created, updated, or deleted THEN the system SHALL invalidate the AdminDashboard cache to reflect updated category statistics

2.2 WHEN tips are created, updated, or deleted THEN the system SHALL invalidate the specific category cache (Category_{categoryId}) to reflect updated tip counts

2.3 WHEN tips are created, updated, or deleted in a category THEN the system SHALL invalidate the CategoryList cache to reflect updated tip counts across all categories

2.4 WHEN users are created or deleted THEN the system SHALL invalidate the AdminDashboard cache to reflect updated user statistics

2.5 WHEN tips are created or deleted THEN the system SHALL invalidate the AdminDashboard cache to reflect updated tip statistics

2.6 WHEN categories are created, updated, or deleted THEN the system SHALL invalidate the CategoryList cache to reflect the updated category list

### Unchanged Behavior (Regression Prevention)

3.1 WHEN cache invalidation is triggered THEN the system SHALL CONTINUE TO use the existing CacheInvalidationService interface and implementation

3.2 WHEN categories are modified THEN the system SHALL CONTINUE TO invalidate the CategoryList cache as currently implemented

3.3 WHEN categories are deleted THEN the system SHALL CONTINUE TO invalidate the specific category cache (Category_{guid}) as currently implemented

3.4 WHEN the cache TTL expires naturally THEN the system SHALL CONTINUE TO refresh cached data on the next request

3.5 WHEN cache keys are referenced THEN the system SHALL CONTINUE TO use the centralized CacheKeys class for key definitions

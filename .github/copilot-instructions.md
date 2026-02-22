
## HttpRepository Placement Pattern (MANDATORY)
* **Implementation Location:** All HttpRepository implementations (e.g., `RegistrationTypeHttpRepository`, `TenantHttpRepository`, `EventAccountTypeHttpRepository`) must reside in `Accredit.Web.Core.Infrastructure/Data/Poc/HttpRepositories/`.
* **Contract Location (POC):** All POC HttpRepository contracts/interfaces must reside in `Accredit.Web.Core.Shared/Abstractions/Data/Poc/HttpRepositories/` (e.g., `IRegistrationTypeHttpRepository`).
* **Contract Location (Non-POC):** Non-POC HttpRepository contracts may live in `Accredit.Web.Core.Shared/Abstractions/HttpRepositories/` (e.g., `IReferenceDataHttpRepository`). Prefer the POC contract folder when the implementation is under `Infrastructure/Data/Poc/HttpRepositories/`.
* **Pattern:**
    - Implementation in Infrastructure for separation of concerns and testability.
    - Contracts in Shared for cross-project consumption and DI registration.
* **Review:** Any new or refactored HttpRepository must follow this pattern. Do not place HttpRepository implementations in the Client project.
* **Rationale:** Ensures clear separation between client-side logic and infrastructure/data access, supports vertical slice architecture, and simplifies dependency injection.

## Frontend/Backend Boundary (MANDATORY)
* **New UI pages:** Build new pages in Blazor WASM under `Accredit.Web.Core.Client` (typically `Accredit.Web.Core/Accredit.Web.Core.Client/Features/...`) and use `@rendermode InteractiveAuto` unless there is a clear, reviewed reason to use Interactive Server.
* **Server communication:** Client pages call the backend via FastEndpoints endpoints in `Accredit.Web.Core.Apis`.
* **Component strategy:** Prefer `Accredit.Web.Core.BaseComponents` for UI components; document new BaseComponents in BlazingStories.

## Data Access Architecture Rules (MANDATORY for New WASM Pages)

### Scope
These data access architecture rules are **MANDATORY for all new WASM pages** built in `Accredit.Web.Core.Client`. Legacy pages and existing features may follow different patterns during migration.

### Primary Pattern
```
UI/Presentation → HTTP Repository → API/Backend → Service → Database (Context) → Return to UI
```

### Exception Pattern (Localizations & AppSettings ONLY)
```
UI/Presentation → Cache Service (for localizations and appsettings only)
```

#### 🚫 CRITICAL VIOLATIONS - NEVER ALLOW

**1. Direct Database Access in UI/Presentation Layer**
- ❌ `DbContext` usage
- ❌ `I*Context` or `*Context` injections (except HttpContext)
- ❌ `SqlConnection`, `SqlCommand`
- ❌ Direct `HttpClient` calls
- ❌ `.ToListAsync()`, `.FirstOrDefaultAsync()` on database entities

**2. Direct Cache Access in UI (Except Localizations/AppSettings)**
- ❌ `ICacheService` for business data
- ✅ ALLOWED ONLY: `localization:*` or `appsettings:*` cache keys

**3. Direct HTTP Calls in UI Components**
- ❌ Direct `HttpClient` usage
- ✅ REQUIRED: Use `I*HttpRepository` injections

#### ✅ CORRECT PATTERNS

**UI/Presentation Layer:**
- ✅ Inject `I*HttpRepository` interfaces ONLY
- ✅ Cache access allowed ONLY for `localization:*` or `appsettings:*` keys
- ✅ No `DbContext`, no `*Context`, no direct HTTP calls

**Service/UseCase Layer:**
- ✅ Inject `ICacheService`
- ✅ Inject `I*HttpRepository` (frontend) or `I*Repository` (backend)
- ✅ Implement cache-aside pattern (check → fetch → store)
- ❌ NO direct `DbContext`, NO direct `HttpClient`

**HTTP Repository Layer:**
- ✅ Inject `HttpClient`
- ✅ Make HTTP calls to API endpoints
- ❌ NO `DbContext`, NO business logic

**Repository Layer (Database Access ONLY):**
- ✅ Inject `DbContext`
- ✅ Contains database queries
- ❌ NO business logic, NO HTTP calls, NO caching

**API Endpoint (FastEndpoints):**
- ✅ Inject `IProcessor`
- ✅ Call `SendMappedOkAsync()`, `SendMappedAsync()`, `SendNotFoundAsync()`
- ✅ Use `BaseEndpoint<Request, Response, Mapper>`
- ❌ NO direct repository calls, NO `DbContext`, NO business logic

### Dependency Injection Rules

**Required Patterns:**
```csharp
// ✅ CORRECT - Interface injection
[Inject] private ITenantHttpRepository TenantHttpRepository { get; set; } = default!;

// ✅ CORRECT - Constructor injection
public TenantService(ITenantRepository repository, ICacheService cache)
{
    _repository = repository;
    _cache = cache;
}
```

**FORBIDDEN Anti-Patterns:**
```csharp
// ❌ NEVER - Direct instantiation
var repository = new TenantRepository();

// ❌ NEVER - Concrete class injection
[Inject] private TenantHttpRepository TenantHttpRepository { get; set; }

// ❌ NEVER - Service locator
var repo = ServiceProvider.GetService<ITenantRepository>();
```

### API Endpoint Implementation Pattern (FastEndpoints)

**Folder Structure (MANDATORY):**
```
Features/
  {Domain}/
    {Verb}/
      V{n}/
        {Entity}.{Verb}.V{n}.Endpoint.cs      ← Endpoint definition
        {Entity}.{Verb}.V{n}.Models.cs        ← Request/Response DTOs
        {Entity}.{Verb}.V{n}.Processor.cs     ← Business logic (IProcessor)
        {Entity}.{Verb}.V{n}.Mapper.cs        ← Entity to Response mapping
```

**Example - Simple Get Operation:**
```
Features/
  Tenants/
    Get/
      V1/
        Tenants.Get.V1.Endpoint.cs
        Tenants.Get.V1.Models.cs
        Tenants.Get.V1.Processor.cs
        Tenants.Get.V1.Mapper.cs
```

**Example - Action-Based Operation:**
```
Features/
  Courses/
    Search/
      Post/
        V1/
          Course.Search.Post.V1.Endpoint.cs
          Course.Search.Post.V1.Models.cs
          Course.Search.Post.V1.Processor.cs
          Course.Search.Post.V1.Mapper.cs
```

**Endpoint Implementation:**
```csharp
namespace Accredit.Web.Core.Apis.Features.Tenants.Get.V1;

using Accredit.Web.Core.Apis.Models.Bases;
using Microsoft.Extensions.Logging;
using static Accredit.Web.Core.Shared.Constants.Constants;

public sealed class Endpoint(IProcessor processor, ILogger<Endpoint> logger) 
    : BaseEndpoint<Request, Response, Mapper>()
{
    public override void Configure()
    {
        Get(ApiUrls.Tenants);                    // ← Use constants, NO hardcoded routes
        Version(1);                               // ← Explicit version
        AllowAnonymous();                         // ← OR AuthSchemes(TokenPrefix.Bearer)
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        logger.LogInformation("Processing request");
        
        var records = await processor.GetRecords(req, ct);
        await SendMappedOkAsync(records, ct);
        
        logger.LogInformation("Request completed successfully");
    }
}
```

**Request/Response Models:**
```csharp
public sealed record Request : BaseRequest
{
    public string? SearchTerm { get; set; }
}

public sealed record Response(HttpStatusCode HttpStatusCode = HttpStatusCode.OK) 
    : BaseResponse(HttpStatusCode)
{
    public required IEnumerable<TenantDto> Records { get; init; } = [];
}
```

**Processor (Business Logic):**
```csharp
public interface IProcessor
{
    Task<IEnumerable<TenantDto>> GetRecords(Request request, CancellationToken ct);
}

[RegisterService]
public sealed class Processor(ITenantService tenantService, ILogger<Processor> logger) 
    : IProcessor
{
    public async Task<IEnumerable<TenantDto>> GetRecords(
        Request request, 
        CancellationToken ct)
    {
        logger.LogInformation("Processing business logic");
        
        var tenants = await tenantService.GetTenants(ct);
        return tenants;
    }
}
```

**Mapper (Response Transformation):**
```csharp
public sealed class Mapper : BaseMapper<Request, Response, Tenant>
{
    public override Task<Response> FromEntityAsync(
        IEnumerable<Tenant> entities, 
        CancellationToken ct = default)
    {
        return Task.FromResult(new Response
        {
            Records = entities.Select(e => new TenantDto
            {
                Id = e.Id,
                Name = e.Name,
                Identifier = e.Identifier
            })
        });
    }
}
```

### Route Constants (MANDATORY)

**All routes MUST use constants from `Constants.ApiUrls`:**

```csharp
// ❌ WRONG - Hardcoded routes
Get("/api/v1/tenants");
Post("/tenants/{id}");

// ✅ CORRECT - Use shared constants
Get(ApiUrls.Tenants);
Post($"{ApiUrls.Tenants}/{{id}}");
```

### Helper Methods on BaseEndpoint

**Success Responses:**
```csharp
await SendMappedOkAsync(entity, ct);           // 200 OK
await SendMappedAsync(entity, 201, ct);        // 201 Created
await SendMappedAsync(entity, 204, ct);        // 204 No Content
```

**Error Responses:**
```csharp
await SendNotFoundAsync(ct);                   // 404 Not Found
await SendUnauthorizedAsync(ct);               // 401 Unauthorized
await SendForbiddenAsync(ct);                  // 403 Forbidden
```

### Code Review Checklist for Data Access

**UI/Presentation Files:**
- [ ] ❌ NO `DbContext` usage
- [ ] ❌ NO `I*Context` injections (except HttpContext)
- [ ] ❌ NO direct `HttpClient` calls
- [ ] ✅ ONLY `I*HttpRepository` injections
- [ ] ✅ Cache allowed ONLY for `localization:*` or `appsettings:*`

**Service/UseCase Files:**
- [ ] ✅ CAN inject `ICacheService`
- [ ] ✅ CAN inject `I*HttpRepository` or `I*Repository`
- [ ] ✅ Implements cache-aside pattern
- [ ] ❌ NO direct `DbContext`
- [ ] ❌ NO direct `HttpClient`

**HTTP Repository Files:**
- [ ] ✅ CAN inject `HttpClient`
- [ ] ✅ Makes HTTP calls to API endpoints
- [ ] ❌ NO `DbContext`
- [ ] ❌ NO business logic

**Repository/Data Access Files:**
- [ ] ✅ CAN inject `DbContext`
- [ ] ✅ Contains database queries
- [ ] ❌ NO business logic
- [ ] ❌ NO HTTP calls
- [ ] ❌ NO caching

**API Endpoint Files:**
- [ ] ✅ Inherits from `BaseEndpoint<Request, Response, Mapper>`
- [ ] ✅ Uses `Configure()` with HTTP verb
- [ ] ✅ Uses route constants
- [ ] ✅ Calls `SendMappedOkAsync()` or similar helpers
- [ ] ❌ NO direct DbContext usage
- [ ] ❌ NO hardcoded routes

## 1. General Principles

* **Prioritize Readability:** Code should be clean, clear, and easy to understand for other developers.
* **Adhere to Existing Styles:** Observe and follow the existing coding style (indentation, naming conventions, brace style, etc.) within the file and project.
* **Conciseness:** Aim for concise and efficient code, but not at the expense of readability.
* **Security First:** When dealing with user input, data storage, or network communication, suggest secure practices (e.g., input validation, parameterized queries, proper error handling, avoiding hardcoded secrets).
* **Performance Awareness:** Consider potential performance implications for critical sections, but optimize only when necessary and justified.
* **Testing Mindset:** Suggest testable code structures. If appropriate, suggest basic unit test structures for new functionality.
* **Documentation:** Refer to the `AI_Documentation/` folder for comprehensive API documentation, implementation patterns, and learnings from previous work. All new API endpoints and significant functionality should be documented following the patterns in this folder.
* **Mandatory Documentation Workflow:** After completing each endpoint, feature, or significant implementation, you MUST:
  1. Update `PLAN.md` - Mark completed items with , add learnings subsection with key patterns and results
  2. Update `SPEC.md` - Add implementation details, document patterns used, note any deviations from spec
  3. Create/Update `AI_Documentation/` - Complete implementation code, patterns, pitfalls, testing approach
  4. Create a commit following the team convention (when you are ready to commit). If a work item exists, prefer `feature/<workitem> <Message> AB#<workitem>`.
  5. This workflow is MANDATORY, not optional - documentation must be updated immediately while knowledge is fresh

## 2. Technology Stack

* **Framework:** .NET 9.0
* **UI Framework:** Blazor (Interactive Server and WebAssembly modes with InteractiveAuto)
* **API Framework:** FastEndpoints for REST APIs with versioning support
* **Database:** SQL Server with Entity Framework Core 9.0
* **ORM:** Entity Framework Core with custom BaseContext pattern
* **Caching:** FusionCache with Redis backplane support
* **Orchestration:** .NET Aspire for local development and service orchestration
* **UI Components:** MudBlazor 8.x with custom component library (Accredit.Web.Core.BaseComponents)
* **Authentication:** ASP.NET Core Identity with JWT tokens for APIs
* **Cloud Services:** AWS (S3 for storage, SSM for configuration)
* **Testing:** xUnit, FluentAssertions, Moq
* **Logging:** log4net with structured logging
* **Reverse Proxy:** YARP (Yet Another Reverse Proxy)

## 3. Repository Structure

### 3.1 Application Projects
* **Accredit.Web.Core** - Main Blazor server application with interactive server components and reverse proxy configuration
* **Accredit.Web.Core.Client** - Blazor WebAssembly client application using InteractiveAuto render mode
* **Accredit.Web.Core.Apis** - FastEndpoints-based REST API project with versioned endpoints (/api/v{version}/...)
* **Accredit.Web.Core.AppHost** - .NET Aspire orchestration application for service management and configuration

### 3.2 Core Library Projects
* **Accredit.Web.Core.Infrastructure** - Data access layer with EF Core contexts, configurations, and base context implementations
* **Accredit.Web.Core.Services** - Business logic services implementing application use cases, inheriting from BaseService<T>
* **Accredit.Web.Core.Shared** - Shared models, entities (all inherit from BaseEntity), DTOs, constants, enums, and abstractions (all interfaces prefixed with 'I')
* **Accredit.Web.Core.BaseComponents** - Reusable Blazor component library (ASButton, ASBanner, ASGrid, ASPagination, etc.)
* **Accredit.Web.Core.ExternalServices** - Integration with third-party services (mail, SSO, AWS S3 storage)
* **Accredit.Web.Core.ServiceDefaults** - Service defaults and shared configurations for .NET Aspire
* **Accredit.Web.Core.Razor** - Legacy Razor MVC views and controllers for backward compatibility with existing system

### 3.3 Supporting Projects
* **Accredit.Web.Core.BlazingStories** - Component documentation and development environment using Blazing Stories pattern
* **Accredit.Web.Core.Tests.Shared** - Shared test utilities, entity builders, mock helpers, and AsyncQueryHelper for testing

### 3.4 Test Projects (all use xUnit, FluentAssertions, Moq)
* **Accredit.Web.Core.Tests** - Tests for main Blazor server application and extensions
* **Accredit.Web.Core.Infrastructure.Tests** - Tests for data access layer and context operations
* **Accredit.Web.Core.Services.Tests** - Tests for business logic services
* **Accredit.Web.Core.ExternalServices.Tests** - Tests for external service integrations
* **Accredit.Web.Core.Shared.Tests** - Tests for shared utilities, extensions, and helpers
* **Accredit.Web.Core.Apis.Tests** - Tests for API endpoints



## 4. Architecture Patterns

### 4.1 Vertical Slice Architecture
The application follows a vertical slice architecture pattern for both Blazor pages and API endpoints:

#### Blazor Features Structure
Features are organized under `/Features/{FeatureName}/` with the following structure:
- **Pages/** - Contains the main page components (.razor and .razor.cs files) with `@page` directive
- **Components/** - Feature-specific components not shared with other features (filters, tables, etc.)
- **Models/** - View models and DTOs specific to this feature only
- **Shared/** - Components shared across multiple features should be placed in `/Features/Shared/Components/`

**Client-first note (current design):** New UI pages and feature work should be added under `Accredit.Web.Core.Client/Features/...` and should be built for WASM (`@rendermode InteractiveAuto`). The server-hosted `Accredit.Web.Core` project contains existing/legacy Interactive Server pages and should not be the default target for new pages.

**Important POC Pattern**: For Proof of Concept features, use `/Features/Poc/{FeatureName}/` structure:
- Modal components that are feature-specific should be placed in `BaseComponents/POC/{FeatureName}/` for reusability
- Use `@namespace Accredit.Web.Core.BaseComponents.POC.{FeatureName}` directive in modal .razor files
- Import modals in pages via `@using Accredit.Web.Core.BaseComponents.POC.{FeatureName}`

**POC Razor pages (MANDATORY):**
- **Authentication required:** Any `.razor` page under a `Poc`/`POC` folder (including nested children) must require an authenticated user (e.g., `@attribute [Authorize]`).
- **Feature gate required:** Any `.razor` page under a `Poc`/`POC` folder must include the `PocAccessGate` component to redirect when the `PocBlazor` setting is turned off.
    - Place `<PocAccessGate />` near the top of the page content so it executes early.

**Example Feature Structure:**
```
Features/
 ApplicantPortal/
    Home/
       Pages/
          Home.razor
          Home.razor.cs
          Home.razor.css
       Models/
           HomeModel.cs
    Shared/
        Components/
            ASPhotoRejectedBanner.razor
 Poc/
    RegistrationTypes/
       Components/
          RegistrationTypeFilters.razor
          RegistrationTypeFilters.razor.css
          RegistrationTypesTable.razor
          RegistrationTypesTable.razor.cs
          RegistrationTypesTable.razor.css
       Models/
          CreateRegistrationTypeDto.cs
          UpdateRegistrationTypeDto.cs
          RegistrationTypeDto.cs
          RegistrationTypeFilterDto.cs
          LookupDto.cs
          PagedResultDto.cs
       Pages/
           RegistrationTypesPage.razor
           RegistrationTypesPage.razor.css
    Contact/
        Models/
        Pages/
```

**BaseComponents POC Structure (for feature-specific modals):**
```
BaseComponents/
 POC/
     RegistrationTypes/
        CreateRegistrationTypeModal.razor
        CreateRegistrationTypeModal.razor.cs
        CreateRegistrationTypeModal.razor.css
        EditRegistrationTypeModal.razor
        EditRegistrationTypeModal.razor.cs
        EditRegistrationTypeModal.razor.css
        StatusChangeModal.razor
        StatusChangeModal.razor.cs
        StatusChangeModal.razor.css
     Localization/
```

#### API Features Structure (FastEndpoints)
API endpoints follow a vertical slice pattern under `Accredit.Web.Core.Apis/Features/...`.

**Folder pattern (in use):**
- `Features/{Domain}/{Verb}/V{n}/...` (e.g., `Features/Tenants/Get/V1/...`)
- `Features/{Domain}/{Action}/{Verb}/V{n}/...` (e.g., `Features/RegistrationTypes/Create/Post/V1/...`, `Features/Notifications/UpdateStatus/Patch/V1/...`)

**Typical files per slice:**
- `*.Endpoint.cs` - endpoint class (prefer inheriting from `BaseEndpoint<...>`)
- `*.Models.cs` - request/response models (`BaseRequest`/`BaseResponse`)
- `*.Processor.cs` - business logic behind an `IProcessor`
- `*.Mapper.cs` - mapper used by `SendMappedAsync(...)`
- `*.Validator.cs` (optional) - FluentValidation validator

**Example API Structure:**
```
Features/
 Nationalities/
    Get/
        V1/
            Nationality.Get.V1.Endpoint.cs
            Nationality.Get.V1.Models.cs
            Nationality.Get.V1.Processor.cs
            Nationality.Get.V1.Mapper.cs
```

### 4.2 Data Access Pattern
* **BaseContext<TEntity>** - Abstract EF Core context providing CRUD operations with predicate filtering
* **Context Classes** - Specific DbContext classes inherit from BaseContext<TEntity> (e.g., NationalityContext)
* **Context Interfaces** - All contexts implement IBaseContext<TEntity> interface in Shared project
* **Scoped Registration** - Contexts registered as scoped services with both concrete and interface types

### 4.3 Service Layer Pattern
* **BaseService<TEntity>** - Abstract service class providing common business logic operations
* **Service Implementations** - Inherit from BaseService<TEntity> and implement specific I{Name}Service interface
* **Constructor Injection** - Services receive contexts and other dependencies via constructor
* **Interface Segregation** - All services have corresponding interfaces in Shared.Abstractions.Services

### 4.4 Dependency Injection Patterns
* **Automatic Registration** - Use `[RegisterService]` attribute with ServiceLifetime parameter for auto-registration in APIs
* **Manual Registration** - Register services in extension methods (AddServices, AddDbContexts) for Blazor apps
* **Context Registration** - DbContexts registered with both concrete type and interface (e.g., AddScoped<INationalityContext, NationalityContext>)
* **Scoped by Default** - Most services and contexts use scoped lifetime unless specified otherwise
## 5. Blazor Component Development

### 5.1 Component Structure
* **Separation of Concerns** - Always use partial classes to separate UI (.razor) from logic (.razor.cs)
* **Component-Scoped Styles** - CSS should reside in a .razor.css file with same name (scoped to component only)
* **Minimal JavaScript** - JavaScript should be used sparingly; prefer Blazor's built-in features. If needed, place in .razor.js file
* **Render Mode** - New Client pages should use `@rendermode InteractiveAuto`. Existing server-hosted pages may use `@rendermode InteractiveServer`.

### 5.2 Component Parameters and Injection
* **[Parameter]** - Use for component inputs from parent components
* **[Parameter(CaptureUnmatchedValues = true)]** - Use Dictionary<string, object> for additional HTML attributes
* **[CascadingParameter]** - Use for shared state or services passed down component hierarchy
* **[Inject]** - Use for dependency injection of services (e.g., `[Inject] private ILocalizationService LocalizationService { get; set; }`)
* **Mandatory Parameters** - Mark required parameters using `required` keyword or validate in OnInitialized

### 5.3 Event Handling
* **EventCallback** - Prefer `EventCallback<T>` for component-to-parent communication
* **Event Propagation** - Control event bubbling with `@onclick:stopPropagation` attribute
* **Async Handlers** - Use async methods for event handlers that perform I/O operations

### 5.4 State Management
* **Component State** - Use private fields for local component state
* **Shared State** - Use cascading parameters or dedicated state services (DI) for shared state
* **StateHasChanged()** - Manual UI updates rarely needed; let Blazor handle automatically where possible

### 5.5 Base Components Library
The project includes a custom component library in `Accredit.Web.Core.BaseComponents`:
* **ASButton** - Custom button with multiple styles (Primary, Secondary, etc.), sizes, icons, and full-width support
* **ASBanner** - Banner component for displaying images with alt text
* **ASGrid** - Grid component for data display (wraps QuickGrid with pagination and selection)
* **ASPagination** - Pagination component
* **ASSelectInput** - Single-select dropdown component
* **ASMultiSelectDropdown** - Multi-select dropdown component
* **ASInputText** - Text input component
* **ASInputCheckbox** - Checkbox component
* **ASInputDate** - Date picker component
* **ASBadge** - Status badge component with color variants
* **ASStyledIcon** - Icon component with styling support
* **ASBaseModal** - Base modal component using MudDialog
* **ASToggle** - Toggle switch component
* Additional components for cards, headers, footers, etc.

**Component Usage Example:**
```razor
<ASButton ButtonType="@ASButton.HtmlButtonType.Submit"
          Color="@ASButton.ButtonStyle.Primary"
          FullWidth="true"
          OnClick="HandleSubmit">
    @LocalizationService.Translate("Submit", "Buttons")
</ASButton>
```

### 5.6 Component Architecture Guidelines

**CRITICAL: Always prefer components from Accredit.Web.Core.BaseComponents over MudBlazor or third-party libraries.**

#### Component Selection Priority (MANDATORY)
1. **First: Check BaseComponents Library** - Always search `Accredit.Web.Core.BaseComponents` for existing components before using MudBlazor or creating new components
2. **Second: Create New in BaseComponents** - If a component doesn't exist in BaseComponents, create it there (following AS naming convention)
3. **Last Resort: MudBlazor** - Only use MudBlazor components when:
   - No alternative exists in BaseComponents
   - Creating a new component would be impractical
   - Document the reasoning in code comments

** COMMENTS **
DO NOT ADD SUMMARY COMMENTS TO ANY CODE!

#### ASGrid Pattern (Primary Grid Component)
ASGrid is the standard grid component that wraps Microsoft.AspNetCore.Components.QuickGrid with enhanced features:

* **ASGrid Wrapper Features**:
  - Built-in pagination UI (ASPagination component)
  - Item selection management (HashSet<int>)
  - Item count chip display
  - Conditional action buttons (shown when items selected)
  - Loading state indicator (MudProgressLinear)

* **Correct ASGrid Usage Pattern**:
```razor
<ASGrid GridName="Registration types" 
        TotalCount="@TotalCount" 
        IsLoading="@IsLoading" 
        Pagination="@Pagination" 
        SelectedItems="@SelectedItems">
    <QuickGrid TGridItem="RegistrationTypeDto" 
               ItemsProvider="@ItemsProvider" 
               Pagination="@Pagination"
               ItemKey="@(item => item.Id)">
        <TemplateColumn Title="Name">
            <div class="checkbox-container">
                <ASInputCheckbox Value="@SelectedItems.Contains(context.Id)"
                                 OnChange="@((bool value) => OnSelectionChanged(context.Id, value))" />
                @context.Name
            </div>
        </TemplateColumn>
        <PropertyColumn Property="@(x => x.EventAccountTypeName)" Title="Functional area" />
        <PropertyColumn Property="@(x => x.TenantName)" Title="Connected tenant" />
        <!-- Additional columns -->
    </QuickGrid>
</ASGrid>
```

* **ASGrid Parameters**:
  - `GridName` (string) - Display name for the grid
  - `TotalCount` (int) - Total number of records for pagination
  - `IsLoading` (bool) - Shows/hides loading indicator
  - `Pagination` (PaginationState) - QuickGrid pagination state
  - `SelectedItems` (HashSet<int>) - Selected item IDs managed by parent
  - `ChildContent` (RenderFragment) - QuickGrid with column definitions

* **QuickGrid Data Loading Pattern**:
```csharp
// In code-behind
private GridItemsProvider<RegistrationTypeDto>? _itemsProvider;
private PaginationState _pagination = new() { ItemsPerPage = 25 };
private HashSet<int> _selectedItems = new();

protected override void OnInitialized()
{
    _itemsProvider = async request =>
    {
        _filter.PageNumber = (request.StartIndex / _pagination.ItemsPerPage) + 1;
        _filter.PageSize = _pagination.ItemsPerPage;
        
        var result = await RegistrationTypeService.SearchAsync(_filter, request.CancellationToken);
        
        return GridItemsProviderResult.From(result.Items, result.TotalRecords);
    };
}

private void OnSelectionChanged(int id, bool isSelected)
{
    if (isSelected)
        _selectedItems.Add(id);
    else
        _selectedItems.Remove(id);
}
```

#### Component Discovery Process
When implementing UI features:
1. Search `Accredit.Web.Core.BaseComponents` project for existing components (use file search or grep)
2. Check component documentation in BlazingStories (if available)
3. Review POC examples in `Features/Poc/` for usage patterns
4. If component doesn't exist, create it in BaseComponents following AS naming convention (e.g., ASComponentName)

#### BlazingStories Documentation Standards (MANDATORY)

**When creating a new BaseComponent (AS*), you MUST generate comprehensive BlazingStories documentation with the following structure. This is non-negotiable and will be reviewed during PR evaluation.**

**Required Story Types (Minimum 8-10 stories total):**

1. **Interactive Story** - Allow users to adjust all component properties in real-time using `@attributes="context.Args"`
   - Enables developers to experiment with different configurations
   - Critical for understanding all parameter combinations

2. **Basic Usage Stories** (2-3 stories minimum)
   - Show primary use cases with different parameter combinations
   - Example: "Label Left - In Progress" and "Label Inline - In Progress"
   - Each story should demonstrate a distinct variant or feature
   - Include specific parameter values (e.g., `ProgressPercentage="50"`)
   - Include accessibility attributes (ARIA, alt text, labels)

3. **State/Variant Stories** (1-2 stories)
   - Demonstrate different states or visual variants
   - Example: "100% Complete - Success State" showing completed vs in-progress states
   - Show color changes, visual indicators, or behavioral differences
   - Side-by-side comparisons when applicable

4. **Edge Cases & Various Levels** (1-2 stories)
   - "Various Progress Levels", "Different Sizes", "Different States", etc.
   - Show 0%, 50%, 100% or equivalent extremes
   - Show decimal values where applicable (e.g., 33.33%, 67.5%)
   - Demonstrate empty states and full states
   - Grid layout with multiple examples for easy comparison

5. **Layout/Responsive Behavior** (1-2 stories)
   - Show layout variants if component supports them
   - "Both Layout Variants Side-by-Side" comparing different layouts
   - Show minimum and maximum width constraints
   - Include border/dashed containers to show dimensions clearly
   - Label sizing and responsive behavior (e.g., 343px min, 672px max)

6. **Content Overflow & Edge Cases** (1-2 stories)
   - "Label Overflow Behavior" - Show how long text is handled
   - Demonstrate truncation, ellipsis, wrapping behavior
   - Test with container width constraints
   - Show graceful degradation

7. **Accessibility Features** (1 story)
   - "ARIA Accessibility Example" or "Keyboard Navigation Example"
   - Show all ARIA attributes in a code block for reference
   - Demonstrate proper semantic HTML
   - Include proper roles, labels, and screen reader announcements
   - Show keyboard interaction if applicable
   - Document what screen readers will announce

8. **Design Tokens Reference** (1 story - MANDATORY)
   - Visual reference showing all colors used in the component
   - Typography details (font family, size, weight, line height)
   - Dimensions (heights, widths, border radius)
   - Spacing and gaps (padding, margins, gaps)
   - Animation details (transition duration, easing, animation properties)
   - Include color swatches with hex values and token names
   - Include a "Live Examples" section showing the component with the design tokens applied
   - Reference Figma specifications where applicable

**Description Format (MANDATORY):**
- **First line:** Clear statement of what the story demonstrates
- **Second line(s):** Specific details about parameter values being used
- **Parameter priority explanation (if applicable):** For components with multiple ways to specify values, clarify which takes priority (e.g., "ProgressPercentage takes priority over Current/Total calculation")
- **Visual/behavioral details:** What the user will observe (colors, layout, interactions)
- **Accessibility notes:** Any ARIA or accessibility features demonstrated

**Example Description:**
```
Progress bar with label positioned below (Label = Left).
Shows 50% progress with "Question 2 of 4" label using ProgressPercentage="50".
ProgressPercentage takes priority over Current/Total calculation (both parameters are shown for reference).
Progress fill uses Brand/700 color (#19395D) when less than 100%.
```

**Code Quality Requirements:**
- Include both simple and complex parameter combinations
- Show default values and optional parameters
- Include `@attributes="context.Args"` in at least the Interactive story to allow runtime customization
- Maintain consistent formatting and indentation
- Use semantic HTML and proper accessibility attributes
- Include visual borders/spacing indicators for layout stories (e.g., `border: 1px dashed #E5E8EB`)

**PR Review Checklist (for reviewing any new AS component):**
- [ ] At least 8-10 stories are included
- [ ] Interactive story allows property adjustment
- [ ] Multiple basic usage examples showing different parameter combinations
- [ ] Edge cases and extreme values demonstrated
- [ ] State/variant stories showing all visual states
- [ ] Layout/responsive stories with dimension indicators
- [ ] Content overflow handling demonstrated
- [ ] Accessibility story with ARIA details and screen reader announcements
- [ ] Design Tokens Reference story with color swatches, typography, dimensions, spacing, animation
- [ ] All descriptions follow the required format with parameter priorities clarified
- [ ] Code examples are correct and follow component patterns
- [ ] No placeholder text or TODOs in stories

**Consistency & Maintenance:**
- BlazingStories should be the single source of truth for component documentation
- Update stories immediately when component parameters or behavior change
- Use stories as the primary reference for developers integrating the component
- Reference the design system/Figma specifications in Design Tokens story

#### MudBlazor Usage Rules
* **Acceptable MudBlazor Components** (used within ASGrid and other BaseComponents):
  - MudDialogProvider - For modal dialog management
  - MudProgressLinear - For loading indicators
  - MudChip - For count/status chips
  - These are acceptable when used INSIDE BaseComponents implementations

* **Avoid in Feature Code**:
  - MudTable, MudDataGrid (use ASGrid instead)
  - MudButton (use ASButton instead)
  - MudTextField (use ASInputText instead)
  - MudSelect (use ASSelectInput or ASMultiSelectDropdown instead)
  - Any MudBlazor form inputs (BaseComponents has AS equivalents)

* **Documentation Requirement**: If using MudBlazor component in feature code, add comment explaining why BaseComponents alternative doesn't exist

#### Examples
See `Features/Poc/Contact/Pages/Contacts.razor` for reference implementation showing correct ASGrid usage with filters, selection, and navigation.

### 5.7 Routing
* **@page Directive** - Ensure `@page` directives are correctly defined (e.g., `@page "/applicant-portal/home"`)
* **Route Parameters** - Define with curly braces (e.g., `@page "/applicant-portal/smart-form/{registrationIdentifier}"`)
* **[Parameter] Binding** - Route parameters must have corresponding `[Parameter]` properties in code-behind

## 6. FastEndpoints API Development

### 6.1 Endpoint Folder Structure & Reusability
* **Top-Level Features Principle** - Each entity gets its own top-level folder under Features/ following vertical slice architecture
* **Reusable Endpoints** - Endpoints used across multiple features MUST be at top-level under Features/, NOT nested under specific features
* **Correct Pattern**: Features/{Domain}/{Verb}/V1/ OR Features/{Domain}/{Action}/{Verb}/V1/ (e.g., Features/EventAccountTypes/Get/V1/, Features/RegistrationTypes/Create/Post/V1/)
* **Wrong Pattern**: Features/{FeatureName}/Lookups/{EntityName}/ (e.g., Features/RegistrationTypes/Lookups/EventAccountTypes/)
* **Decision Rule**: Ask "Will other features use this endpoint?" If yes  top-level placement under Features/{EntityName}/
* **Examples**:
  -  Features/EventAccountTypes/ - Used by RegistrationTypes, Events, Contacts
  -  Features/Tenants/ - Used by multiple features across application
  -  Features/RegistrationTypes/ - Specific to RegistrationTypes feature
  -  Features/RegistrationTypes/Lookups/EventAccountTypes/ - Couples EventAccountTypes to RegistrationTypes
* **Test Structure Mirrors API** - Test files follow same folder structure (e.g., Tests/Features/EventAccountTypes/Get/V1/)

### 6.2 Endpoint Implementation
* **Inheritance (preferred)** - Use `BaseEndpoint<TRequest, TResponse, TMapper>` (or `BaseEndpoint<TRequest>`) for consistent mapped responses and shared helpers.
* **Allowed exception** - Use raw `FastEndpoints.Endpoint<TRequest, TResponse>` in special cases (e.g., internal API-key protected endpoints using custom PreProcessors and manual `SendAsync`), while keeping the same vertical slice layout.
* **Configure Method** - Define HTTP verb, route, version, and authorization in `Configure()` method
* **HandleAsync Method** - Implement request handling logic, calling processor and using `SendMappedOkAsync()` or similar

**Routes and constants (MANDATORY):**
- Use `Accredit.Web.Core.Shared.Constants.Constants.ApiUrls` for endpoint routes (do not hardcode route strings).
- Do not include `api/` or version prefixes in `ApiUrls` values; `Accredit.Web.Core.Apis/Program.cs` applies `RoutePrefix = "api"` and versioning (`/v{n}`) globally.
- Prefer route constants without a leading slash (e.g., `"tenants"`, not `"/tenants"`). Some existing constants include leading `/`; avoid adding new ones.

**Example Endpoint:**
```csharp
public sealed class Endpoint(IProcessor processor) 
    : BaseEndpoint<Request, Response, Mapper>()
{
    public override void Configure()
    {
        Get("/nationality");
        Version(1);
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var records = await processor.GetRecords(req, ct);
        await SendMappedOkAsync(records, ct);
    }
}
```

### 6.3 Request and Response Models
* **BaseRequest** - All request models inherit from `BaseRequest`
* **BaseResponse** - All response models inherit from `BaseResponse` with HttpStatusCode
* **Records Pattern** - Use sealed records for immutability
* **Required Properties** - Use `required` keyword for mandatory response properties

**Example Models:**
```csharp
public sealed record Request : BaseRequest
{
    public int Id { get; set; }
}

public sealed record Response(HttpStatusCode HttpStatusCode = HttpStatusCode.OK) 
    : BaseResponse(HttpStatusCode)
{
    public required IEnumerable<Nationality> Records { get; init; } = [];
}
```

### 6.4 Processors
* **IProcessor Interface** - Define processor interface for business logic
* **[RegisterService] Attribute** - Use for automatic DI registration with optional ServiceLifetime
* **BaseProcessor** - Inherit from BaseProcessor<TRequest, TEntity> for common patterns
* **Context Injection** - Inject specific context interfaces (e.g., INationalityContext)

**Example Processor:**
```csharp
[RegisterService]
public sealed class Processor(INationalityContext context) 
    : BaseProcessor<Request, Nationality>, IProcessor
{
    public override async Task<IEnumerable<Nationality>> GetRecords(
        Request request, 
        CancellationToken ct = default)
    {
        IQueryable<Nationality> query = context.Nationalities
            .OrderBy(x => x.NationalityName);
        return query;
    }
}
```

### 6.5 Mappers
* **BaseMapper** - Inherit from `BaseMapper<TRequest, TResponse, TEntity>`
* **FromEntity Method** - Implement mapping from entity/data to response

### 6.6 Validators (Optional)
* **FluentValidation** - Use FluentValidation for request validation
* **BaseValidator** - Inherit from BaseValidator<TRequest> when needed

### 6.7 API Configuration
* **Route Prefix** - All endpoints use `/api` prefix
* **Versioning** - Use `Version(n)` in Configure() for versioned endpoints (/api/v1/, /api/v2/, etc.)
* **Serialization** - Property naming policy is null (preserves casing)
* **Validation** - `DontThrowIfValidationFails()` is configured globally

## 7. C# Coding Standards

### 7.1 Modern C# Features
* **Target Framework** - Always use .NET 9.0
* **File-Scoped Namespaces** - Prefer file-scoped namespaces for new files; do not churn existing brace-style namespaces unless you"re already touching the file for functional work.
* **Namespace Placement** - Place using statements inside namespace declaration
* **Primary Constructors** - Use primary constructors for dependency injection when appropriate
* **Record Types** - Use record types for DTOs, requests, and responses
* **Required Properties** - Use `required` keyword for mandatory properties

**Example:**
```csharp
namespace Accredit.Web.Core.Services;

using Accredit.Web.Core.Services.Base;

public class MyService(IMyContext context) : BaseService<MyEntity>(context)
{
    // Implementation
}
```

### 7.2 Naming Conventions
* **PascalCase** - Use for class names, method names, properties, and public members
* **camelCase** - Use for local variables, parameters, and private fields
* **Interface Prefix** - All interfaces must be prefixed with "I" (e.g., ISmartFormService)
* **Private Fields** - Prefix with underscore "_fieldName"

### 7.3 Code Structure
* **Guard Clauses** - Prefer guard clauses over else statements
* **Braces** - ** IMPORTANT ** MUST Always use braces for if, else, for, foreach, while, and do-while statements. Braces are mandatory even for single-line blocks.

```csharp
if (condition)
{
    // code
}
else
{
    // code
}
for (var i = 0; i < n; i++)
{
    // code
}
```

** IMPORTANT ** Do not omit braces for any conditional or loop block.
* **Magic Numbers** - Avoid magic numbers; use named constants instead
* **Method Length** - Keep methods focused and avoid deeply nested logic
* **Variable Declaration** - Use `var` when type is obvious from right side

### 7.4 Async/Await
* **Async Methods** - Always use `async`/`await` for I/O-bound operations
* **CancellationToken** - Include CancellationToken parameter for long-running operations
* **Naming** - Async methods should end with "Async" suffix

### 7.5 Access Modifiers
* **const** - Use for compile-time constants
* **readonly** - Use for fields not modified after construction
* **private** - Use for implementation details not needed outside class
* **public** - Use for properties/methods accessed outside class
* **protected** - Use for members accessed in derived classes
* **internal** - Use for members accessible within same assembly

## 8. Unit Testing Standards

### 8.1 General Testing Principles
* **Test Framework** - Use xUnit, FluentAssertions, and Moq for all unit tests
* **Code Coverage (MANDATORY)** - Maintain at least 90% code coverage for new/changed code (prefer adding focused tests rather than excluding code from coverage)
* **Test Location** - Place tests in separate "ProjectName.Tests" project matching the source project
* **Test Generation** - Always generate unit tests for new code; update tests when modifying existing code
* **ExcludeFromCodeCoverage** - Mark non-testable classes (e.g., Program.cs, startup classes) with `[ExcludeFromCodeCoverage]` attribute

### 8.2 Test Structure
* **Naming Convention** - Use format `ClassName_MethodName_ExpectedBehavior`
* **AAA Pattern (MANDATORY)** - Follow Arrange-Act-Assert pattern consistently
* **Assertion Scope** - Wrap multiple assertions in `using (new AssertionScope())` block
* **Test Data** - Create separate `TestData` class in same namespace for test data

**Assertions and mocking (MANDATORY):**
* **FluentAssertions** - Use FluentAssertions v7 style assertions.
* **Mocks** - Use Moq for mocking (do not introduce alternative mocking frameworks).

**Example Test:**
```csharp
[Fact]
public async Task GetApplicantSmartFormOrDefaultUrl_UseDefaultSmartFormTrue_ReturnsApplicantPortalUrl()
{
    // Arrange
    var registrationIdentifier = TestData.ValidRegistrationIdentifier;
    var configValue = "true";
    
    _configurationDataServiceMock
        .Setup(x => x.GetApplicantPortalConfigurationValue(GeneralConfigurationKey.UseDefaultSmartForm))
        .ReturnsAsync(configValue);
    
    // Act
    var result = await _smartFormService.GetApplicantSmartFormOrDefaultUrl(registrationIdentifier);
    
    // Assert
    using (new AssertionScope())
    {
        result.Should().Be($"/Registration/Home/ApplicantPortalReg?registrationIdentifier={registrationIdentifier}");
        _configurationDataServiceMock.Verify(
            x => x.GetApplicantPortalConfigurationValue(GeneralConfigurationKey.UseDefaultSmartForm), 
            Times.Once);
    }
}
```

### 8.3 Mocking Entity Framework
* **DbSet Mocking** - Use `AsAsyncMockDbSet()` extension method from `Accredit.Web.Core.Tests.Shared.AsyncQueryHelper`
* **Async Operations** - Ensure mocks support async Entity Framework Core operations
* **Consistency** - Use shared helper methods for consistent DbSet mocking across tests

**Example:**
```csharp
var mockDbSet = entities.AsAsyncMockDbSet();
_mockContext.Setup(x => x.Entities).Returns(mockDbSet.Object);
```

### 8.4 Test Organization
* **Constructor Setup** - Initialize mocks and system under test in constructor
* **Private Fields** - Use readonly private fields for mocks (e.g., `private readonly Mock<IService> _serviceMock`)
* **Fact vs Theory** - Use `[Fact]` for single test cases, `[Theory]` with `[InlineData]` for parameterized tests

## 9. Data Access Layer

### 9.1 Entity Design
* **BaseEntity** - All entities inherit from `BaseEntity` which provides common properties (Id, Identifier, etc.)
* **DbSet Properties** - Expose DbSet<TEntity> properties in context classes
* **Navigation Properties** - Use EF Core navigation properties for relationships

### 9.2 Context Pattern
* **BaseContext<TEntity>** - Provides strongly-typed CRUD operations with predicate filtering
* **Interface Implementation** - Context implements both concrete class and IBaseContext<TEntity> interface
* **Configuration** - Use separate configuration classes in `Data/Context/Configuration` folder
* **Factory Pattern** - Some contexts use `AddDbContextFactory<T>` for concurrent access scenarios

**Example Context:**
```csharp
namespace Accredit.Web.Core.Infrastructure.Data.Context;

using Accredit.Web.Core.Infrastructure.Data.Contexts.Base;
using Accredit.Web.Core.Shared.Abstractions.Data.Context;
using Accredit.Web.Core.Shared.Entities;
using Microsoft.EntityFrameworkCore;

public class NationalityContext(DbContextOptions<NationalityContext> options) 
    : BaseContext<Nationality>(options), INationalityContext
{
    public DbSet<Nationality> Nationalities { get; set; }
}
```

### 9.3 Context Registration
* **Both Types** - Register context as both concrete type and interface
* **Scoped Lifetime** - Use scoped lifetime for DbContext instances
* **Connection String** - Pass connection string from configuration

**Example Registration:**
```csharp
services.AddDbContext<NationalityContext>(options => 
    options.UseSqlServer(connectionString));
services.AddScoped<INationalityContext, NationalityContext>();
```

## 10. Service Layer

### 10.1 Service Design
* **BaseService<TEntity>** - Inherit from BaseService<TEntity> for common operations
* **Interface Implementation** - Implement specific I{Name}Service interface from Shared.Abstractions.Services
* **Constructor Injection** - Inject context and other dependencies via constructor
* **Validation** - Use protected ValidateEntityForCreate/Update methods

**Example Service:**
```csharp
namespace Accredit.Web.Core.Services;

using Accredit.Web.Core.Services.Base;
using Accredit.Web.Core.Shared.Abstractions.Data.Context;
using Accredit.Web.Core.Shared.Abstractions.Services;
using Accredit.Web.Core.Shared.Entities.SmartForms;

public class SmartFormService : BaseService<SmartForm>, ISmartFormService
{
    private readonly ISmartFormContext _smartFormContext;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IStorage _storage;

    public SmartFormService(
        ISmartFormContext context, 
        IConfigurationDataService configurationDataService,
        IStorage storage) : base(context)
    {
        _smartFormContext = context;
        _configurationDataService = configurationDataService;
        _storage = storage;
    }

    public async Task<string> GetApplicantSmartFormOrDefaultUrl(string registrationIdentifier)
    {
        // Implementation
    }
}
```

### 10.2 Service Registration
* **Manual Registration** - Register services manually in Blazor apps via extension methods
* **Automatic Registration** - Use `[RegisterService]` attribute in API projects
* **Scoped Lifetime** - Most services use scoped lifetime by default

## 11. Error Handling & Logging

### 11.1 Exception Handling
* **Try-Catch Blocks** - Use for expected errors only
* **Specific Exceptions** - Catch specific exception types; avoid catching generic `Exception` unless rethrowing
* **Validation** - Validate inputs early with guard clauses

### 11.2 Logging
* **ILogger<T>** - Inject `ILogger<T>` for structured logging
* **Log Levels** - Use appropriate levels: LogError, LogWarning, LogInformation, LogDebug
* **log4net** - Application uses log4net configuration for centralized logging
* **Structured Data** - Include relevant context in log messages

**Example:**
```csharp
[Inject] private ILogger<SmartFormPage> Logger { get; set; } = default!;

try
{
    // Operation
}
catch (Exception ex)
{
    Logger.LogError(ex, "Error fetching banner URL");
    // Handle error
}
```

## 12. Configuration and Environment

### 12.1 Configuration Sources
* **appsettings.json** - Base configuration
* **appsettings.Development.json** - Development overrides
* **AWS SSM** - Production secrets and configuration via AWS Systems Manager Parameter Store
* **Environment Variables** - Runtime configuration

### 12.2 .NET Aspire
* **AppHost Project** - Orchestrates all services and dependencies
* **ServiceDefaults** - Shared service configuration (telemetry, health checks, etc.)
* **Local Development** - Simplifies running multiple projects simultaneously

### 12.3 Caching
* **FusionCache** - Primary caching abstraction with multilevel caching support
* **Redis Backplane** - Distributed cache invalidation across instances
* **IFusionCacheResolver** - Service for resolving named cache instances

## 13. AI Documentation & Learnings

### 13.1 AI_Documentation Folder Structure
The `AI_Documentation/` folder contains comprehensive documentation, implementation patterns, and learnings:
* **API_Endpoints/** - Detailed documentation for each API endpoint with complete implementation code, patterns, testing approaches, and learnings
  - `RegistrationType_Create.md` - Create endpoint patterns (VerifyRequestData, ValidationContext, error codes, context usage)
  - `RegistrationType_Search.md` - Search/pagination patterns (tuple returns, dynamic sorting, filtering)
  - `RegistrationType_GetById.md` - GetById patterns (null handling, navigation properties, FirstOrDefaultAsync)
  - Additional endpoint documentation as endpoints are created

### 13.2 FastEndpoints Patterns (from AI_Documentation)
* **VerifyRequestData Signature** - Does NOT take CancellationToken parameter in BaseProcessor: `Task VerifyRequestData(Request request)`
* **ValidationContext Pattern** - Uses `ValidationContext<Request>.Instance` for complex validation, but cannot be unit tested (requires service resolver)
* **ValidationContext Limitation** - `ValidationContext<T>.Instance` requires FastEndpoints service resolver configuration; cannot be easily unit tested in isolation. Test validation logic via Validator tests (FluentValidation) instead
* **Entity Creation** - Use `context.CreateEntityAsync(entity, ct)` from BaseContext, not `AddAsync` + `SaveChangesAsync`
* **Tuple Returns** - Use `(Pagination pagination, IEnumerable<T> entities)` for paginated search results
* **Navigation Properties** - Load with `.Include()` early in query before filters/sorting/pagination
* **FirstOrDefaultAsync** - Preferred over FindAsync when navigation properties are needed (FindAsync doesn"t support Include)
* **Deferred Execution** - Return IQueryable from Processor, materialization happens in Mapper/response serialization

### 13.3 Error Handling Patterns
* **Error Codes as SmartEnum** - Add error codes to `ErrorCodes.cs` with Name, Message, and HttpStatusCode
* **ValidationFailureException** - FastEndpoints throws this from `ValidationContext.ThrowIfAnyErrors()`, but does not expose `.Errors` property
* **HTTP Status Codes**:
  - 200 OK - Use `SendMappedOkAsync(record, ct)`
  - 201 Created - Use `SendMappedAsync(record, statusCode: 201, ct: ct)`
  - 404 Not Found - Use `SendNotFoundAsync(ct)` when entity not found
  - 409 Conflict - Return via ValidationFailureException for duplicates

### 13.4 Entity Framework Patterns
* **EF.Functions.Collate** - Use `EF.Functions.Collate(value, "SQL_Latin1_General_CP1_CI_AI")` for case-insensitive searches in SQL Server
* **Collate Testing Limitation** - Cannot be tested with in-memory mocked DbSets; remove `.Collate()` calls in unit tests or use integration tests
* **Audit Fields** - Always set `CreatedDate` and `Identifier` on entity creation
* **Required Properties** - Entity properties marked `required` must be initialized even in test scenarios
* **Context Extensions** - Extend IContext interface and implementation with related DbSet properties needed for validation (e.g., EventAccountTypes, Tenants)

### 13.5 Testing Patterns
* **Unit Test Coverage** - Aim for 90%+ code coverage across all projects
* **Test Structure** - Use AAA pattern (Arrange-Act-Assert) with AssertionScope for multiple assertions
* **DbSet Mocking** - Use `AsAsyncMockDbSet()` extension from `Accredit.Web.Core.Tests.Shared.AsyncQueryHelper`
* **Validator Tests** - Test FluentValidation rules separately from business logic
* **VerifyRequestData Tests** - Cannot be unit tested due to ValidationContext limitation; validation is tested via Validator tests instead
* **Business Logic Tests** - Test Create/Update/Process methods with mocked contexts
* **Mapper Tests** - Test entity-to-response mapping including null handling

### 13.6 Documentation Requirements
When creating new endpoints or significant functionality:
1. Create markdown documentation in `AI_Documentation/` folder
2. Include complete implementation code for all files
3. Document key patterns and critical learnings
4. Provide request/response examples
5. Document testing approach and results
6. List common pitfalls and solutions
7. Include performance considerations
8. Cross-reference related documentation

## 14. Recent Learnings & Enforcement Patterns

### 14.1 Razor Code-Behind Enforcement
* **MANDATORY (production UI):** For UI in `Accredit.Web.Core.Client`, `Accredit.Web.Core` (server app), and `Accredit.Web.Core.BaseComponents`, use partial class code-behind files (`.razor.cs`) for logic. Do not introduce new inline `@code {}` blocks.
* **Allowed exception (documentation/demo):** BlazingStories story files may use small inline `@code` blocks for demo state.
* **Pattern:** For every `.razor` file, create a corresponding `.razor.cs` partial class. Move all logic, event handlers, and state management to the code-behind file.
* **Review:** Any new or refactored feature must be checked for compliance. Inline code blocks should be flagged and refactored immediately.

### 14.2 FastEndpoints DTO Binding Requirement
* **MANDATORY:** All FastEndpoints Request DTOs must declare at least one public property. Empty records or classes will cause binding errors at runtime.
* **Pattern:** If the request does not require any properties, add a dummy property (e.g., `public int? Placeholder { get; set; }`) with a comment explaining its purpose.
* **Review:** Compare with working endpoints (e.g., Nationality) to ensure DTOs are correctly structured. Refactor any endpoints that do not comply.

### 14.3 Dependency Injection Naming Consistency
* **Pattern:** Always use consistent DI property names (e.g., `RegistrationTypeHttpRepository`), matching interface and implementation names. Avoid ambiguous names like `RegistrationTypeService`.

### 14.4 API URL and Constants Usage
* **Pattern:** Use shared constants for API URLs and endpoint routes. Avoid hardcoded strings in endpoint and repository classes. Update all usages to reference the shared constants.

### 14.5 Documentation Workflow Reminder
* **MANDATORY:** After any endpoint or feature refactor, update `PLAN.md`, `SPEC.md`, and `AI_Documentation/` with learnings, implementation details, and patterns used. This ensures Copilot and developers retain knowledge and do not deviate from established practices.

## 15. Running the Application Locally

Before running locally kill all dotnet processes to avoid port conflicts.
taskkill /IM "dotnet.exe" /F

When you run this solution you must run both the API project and the Blazor project simultaneously for the application to function correctly.

Use the following steps to run both projects locally:

To Run the api solution:
cd "Accredit.Web.Core.Apis"; dotnet run --urls="https://localhost:7151"

To Run the Blazor solution:
cd "Accredit.Web.Core\Accredit.Web.Core"; dotnet run --urls="https://localhost:7150"

Do NOT run the Accredit.Web.Core.AppHost project

Monitor the console output for both projects to ensure they start successfully without errors. Once both projects are running, you can access the Blazor application at https://localhost:7150, which will communicate with the API at https://localhost:7151.


## 16. Design System Documentation & BaseComponents BlazingStories Enforcement

### 16.1 Design_Brand.md Review Requirement
* **MANDATORY:** When creating any new Razor pages or components (including BaseComponents) based on Figma designs, LLMs and developers MUST review the latest `AI_Documentation/Design_Brand.md` file.
* **Pattern:** All design tokens, color mappings, typography, spacing, shadow, and component patterns must be cross-referenced with the documentation in `Design_Brand.md`.
* **Review:** Copilot and reviewers must verify that new UI work aligns with the documented design system and implementation notes in `Design_Brand.md`. Any deviation must be justified in code comments and PR description.

### 16.2 BaseComponents & BlazingStories Documentation Enforcement
* **MANDATORY:** When creating any new BaseComponent with a name beginning with `AS` (e.g., `ASButton`, `ASBaseCard`, etc.), the component MUST be added to the `Accredit.Web.Core.BlazingStories` project for documentation and discoverability.
* **Pattern:** Each new `AS*` BaseComponent must have a corresponding story/demo in BlazingStories, following the established documentation pattern.
* **Review:** Copilot must check all new/modified PRs for new `AS*` components in `Accredit.Web.Core.BaseComponents`. If a new component is found and is not documented in BlazingStories, Copilot must highlight this in the PR review and request the addition before approval.
* **Rationale:** This ensures all custom components are documented, discoverable, and tested in isolation, supporting design system consistency and developer onboarding.

### 16.3 BlazingStories Standard - Comprehensive Documentation Requirements (MANDATORY)

All BlazingStories documentation for BaseComponents MUST follow this comprehensive standard. The ASTooltip story serves as the gold standard reference example.

#### 16.3.1 Required Stories Structure (MANDATORY)

Every BaseComponent BlazingStories file MUST include the following stories:

**1. Interactive Story (REQUIRED)**
- MUST be the first story in the file
- MUST use `@attributes="context.Args"` to expose all component parameters
- MUST include a comprehensive `<Description>` tag explaining:
  - What the interactive story allows users to do
  - Which properties can be adjusted
  - Key behavior or constraints of the component
- MUST use appropriate container classes from the `.stories.razor.css` file

**Example:**
```razor
<Story Name="Interactive">
    <Description>
        Interactive tooltip where you can adjust all properties.
        Use the controls below to experiment with different settings including Position and MaxWidth.
        The tooltip always appears ABOVE the trigger element. The Position parameter controls where the arrow is positioned on the tooltip.
    </Description>
    <Template>
        <div class="story-container-tooltip">
            <ASTooltip @attributes="context.Args">
                <ASButton ButtonType="@ASButton.HtmlButtonType.Button"
                          Color="@ASButton.ButtonStyle.Primary">
                    Hover or focus me
                </ASButton>
            </ASTooltip>
        </div>
    </Template>
</Story>
```

**2. Variant Stories (REQUIRED)**
- MUST include separate stories for each major variant or configuration of the component
- Each variant MUST have a descriptive name (e.g., "Bottom Centre Arrow", "Primary Button", "Success Badge")
- Each variant MUST include a `<Description>` tag explaining:
  - What this variant demonstrates
  - Key visual or functional characteristics
  - When to use this variant
  - Design token references (colors, spacing) where applicable

**Example:**
```razor
<Story Name="Bottom Centre Arrow">
    <Description>
        Tooltip with arrow positioned at the bottom centre.
        The tooltip appears ABOVE the trigger element with the arrow tip aligned to the centre.
        This is the default position. Uses light theme with Base/White (#FCFDFE) background and Neutrals/700 (#3B4048) text.
    </Description>
    <Template>
        <div class="story-container-tooltip">
            <ASTooltip TooltipContent="Example tooltip" Position="TooltipPosition.BottomCentre">
                <ASButton ButtonType="@ASButton.HtmlButtonType.Button"
                          Color="@ASButton.ButtonStyle.Primary">
                    Hover me (Centre Arrow)
                </ASButton>
            </ASTooltip>
        </div>
    </Template>
</Story>
```

**3. Comparison Story (REQUIRED for components with variants)**
- MUST show multiple variants side-by-side for visual comparison
- MUST use grid or flex layouts with appropriate classes
- MUST include labels or captions identifying each variant
- MUST include a `<Description>` explaining what is being compared and why

**Example:**
```razor
<Story Name="All Arrow Positions Side-by-Side">
    <Description>
        Comparison of all three tooltip arrow positions (BottomLeft, BottomCentre, BottomRight) at the same time.
        Demonstrates how Position parameter affects arrow placement on the tooltip.
        The tooltip always appears ABOVE the trigger, only the arrow position changes.
        All tooltips use default MaxWidth (200px) and show on hover/focus interactions.
    </Description>
    <Template>
        <div class="story-container-side-by-side">
            <div class="grid-3-col">
                <div class="col-align-bottom">
                    <ASTooltip TooltipContent="Arrow on left" Position="TooltipPosition.BottomLeft">
                        <ASButton ButtonType="@ASButton.HtmlButtonType.Button"
                                  Color="@ASButton.ButtonStyle.Primary">
                            Bottom Left
                        </ASButton>
                    </ASTooltip>
                    <p class="label-caption">BottomLeft</p>
                </div>
                <!-- Additional columns -->
            </div>
        </div>
    </Template>
</Story>
```

**4. Edge Case Stories (REQUIRED)**
- MUST demonstrate edge cases like:
  - Long content/text wrapping
  - Truncation behavior
  - Empty states
  - Disabled states
  - Loading states
  - Error states
- Each edge case MUST have its own story with detailed description

**Example:**
```razor
<Story Name="Long Content - Text Wrapping">
    <Description>
        Tooltip with longer text content demonstrating text wrapping behavior.
        Uses default MaxWidth="200" which allows text to wrap within constraint.
        Text wraps naturally with white-space: normal, maintaining readability.
        Content automatically truncates at 150 characters to prevent overly long tooltips.
        Light theme: Base/White (#FCFDFE) background, Neutrals/700 (#3B4048) text.
    </Description>
    <Template>
        <div class="story-container-tooltip">
            <ASTooltip TooltipContent="This is a longer tooltip text that demonstrates how the tooltip handles multiple lines of content and wraps properly within the maximum width constraint.">
                <ASButton ButtonType="@ASButton.HtmlButtonType.Button"
                          Color="@ASButton.ButtonStyle.Primary">
                    Hover me (Long content)
                </ASButton>
            </ASTooltip>
        </div>
    </Template>
</Story>
```

**5. Real-World Usage Stories (REQUIRED)**
- MUST show the component in realistic usage scenarios
- MUST demonstrate integration with other components
- MUST show different trigger elements or container contexts
- Each scenario MUST have a description explaining the use case

**Example:**
```razor
<Story Name="Various Trigger Elements">
    <Description>
        Demonstrates tooltips attached to different types of trigger elements.
        Shows flexibility of the component with buttons, icons, badges, and text.
        All triggers support hover and focus interactions consistently.
    </Description>
    <Template>
        <div class="story-container-vertical-list">
            <div class="list-item-first">
                <span class="list-label">Primary Button:</span>
                <ASTooltip TooltipContent="Click to submit the form">
                    <ASButton ButtonType="@ASButton.HtmlButtonType.Button"
                              Color="@ASButton.ButtonStyle.Primary">
                        Submit
                    </ASButton>
                </ASTooltip>
            </div>
            <!-- Additional items -->
        </div>
    </Template>
</Story>
```

**6. ARIA Accessibility Story (REQUIRED)**
- MUST document complete accessibility implementation
- MUST show ARIA attributes used (role, aria-hidden, aria-label, etc.)
- MUST explain keyboard accessibility (Tab navigation, focus states)
- MUST describe screen reader announcements
- MUST include a visual panel showing the ARIA markup

**Example:**
```razor
<Story Name="ARIA Accessibility Example">
    <Description>
        Demonstrates proper accessibility implementation with role="tooltip".
        Tooltip has aria-hidden state that toggles on show/hide.
        Trigger element is keyboard accessible (can be focused with Tab).
        Screen readers announce tooltip content when trigger receives focus.
        Follows ARIA Tooltip pattern for optimal accessibility.
    </Description>
    <Template>
        <div class="story-container-accessibility">
            <div class="info-panel">
                <h4 class="info-panel-title">ARIA Attributes</h4>
                <pre class="code-block">Tooltip:
  role="tooltip"
  aria-hidden="true" (when hidden)
  aria-hidden="false" (when visible)

Trigger (button):
  Standard button semantics
  Keyboard accessible (Tab to focus)
  
Screen Reader Announcement:
  "Save changes, button"
  "This will permanently save your changes to the database" (on focus)</pre>
            </div>
            <div class="centered-preview">
                <ASTooltip TooltipContent="This will permanently save your changes to the database">
                    <ASButton ButtonType="@ASButton.HtmlButtonType.Button"
                              Color="@ASButton.ButtonStyle.Primary">
                        Save changes
                    </ASButton>
                </ASTooltip>
            </div>
        </div>
    </Template>
</Story>
```

**7. Design Tokens Reference Story (REQUIRED)**
- MUST be the final story in the file
- MUST document ALL design tokens used by the component including:
  - **Colors**: All colors with exact hex/rgba values and Figma variable names
  - **Typography**: Font family, size, weight, line height
  - **Dimensions**: Width, height, border radius, max/min constraints
  - **Spacing**: Padding, margin, gaps
  - **Shadows**: Complete shadow specifications (all layers)
  - **Positioning**: Z-index, positioning behavior
  - **States**: Hover, focus, disabled, active states
- MUST include visual color swatches for each color token
- MUST include live examples at the bottom of the token documentation

**Example:**
```razor
<Story Name="Design Tokens Reference">
    <Description>
        Visual reference for the design tokens used in the ASTooltip component.
        Colors, typography, spacing, and dimensions match Figma specifications.
    </Description>
    <Template>
        <div class="story-container-tokens">
            <div class="grid-tokens">
                
                <!-- Colors -->
                <div class="token-card">
                    <h4 class="token-card-title">Colors (Figma Design)</h4>
                    <div class="token-list">
                        <div class="token-item">
                            <div class="color-swatch swatch-background"></div>
                            <div class="token-details">
                                <div class="token-name">Background</div>
                                <div class="token-value">Base/White - #FCFDFE</div>
                            </div>
                        </div>
                        <!-- Additional color tokens -->
                    </div>
                </div>

                <!-- Typography -->
                <div class="token-card">
                    <h4 class="token-card-title">Typography (Text xs/Semibold)</h4>
                    <div class="typography-details">
                        <div class="typography-item"><strong>Font Family:</strong> Roboto</div>
                        <div class="typography-item"><strong>Font Size:</strong> 12px</div>
                        <div class="typography-item"><strong>Font Weight:</strong> 600 (Semibold)</div>
                        <div><strong>Line Height:</strong> 18px</div>
                    </div>
                </div>

                <!-- Additional token sections -->
            </div>

            <!-- Live Examples -->
            <div class="live-examples-panel">
                <h4 class="live-examples-title">Live Examples</h4>
                <div class="live-examples-container">
                    <!-- Live component examples demonstrating tokens -->
                </div>
            </div>
        </div>
    </Template>
</Story>
```

#### 16.3.2 Scoped CSS File Requirement (MANDATORY)

- **MANDATORY:** Every BlazingStories file MUST have a companion `.stories.razor.css` file
- **NO inline styles:** Do not use inline `style=""` attributes in story templates
- **Scoped classes:** All CSS classes automatically scope to the story file
- **Consistent naming:** Use semantic class names (e.g., `story-container-tooltip`, `grid-3-col`, `token-card`)

**Required CSS patterns:**
```css
/* Container classes for different layout needs */
.story-container-tooltip { /* Centered with padding */ }
.story-container-side-by-side { /* Flex/grid layout */ }
.story-container-vertical-list { /* Vertical stacking */ }
.story-container-tokens { /* Design tokens grid */ }
.story-container-accessibility { /* Accessibility demo */ }

/* Grid layouts */
.grid-3-col { /* 3-column grid */ }
.grid-tokens { /* Responsive token grid */ }

/* Token documentation classes */
.token-card { /* Card for each token category */ }
.token-card-title { /* Section headers */ }
.token-list { /* List of tokens */ }
.token-item { /* Individual token row */ }
.color-swatch { /* Color preview squares */ }
.token-details { /* Token name/value */ }

/* Helper classes */
.label-caption { /* Small descriptive text */ }
.info-panel { /* Information boxes */ }
.code-block { /* Preformatted code */ }
.live-examples-container { /* Live example grid */ }
```

#### 16.3.3 Description Requirements (MANDATORY)

Every `<Story>` tag MUST include a `<Description>` that provides:

1. **What:** Clear statement of what this story demonstrates
2. **How:** Explanation of key parameters, props, or configurations shown
3. **Why:** Context for when/why to use this variant or pattern
4. **Details:** Specific design token values referenced (colors, spacing, typography)
5. **Behavior:** Any important interaction patterns or constraints

**Example Description Checklist:**
- ✅ Explains the specific variant/feature being demonstrated
- ✅ Mentions key parameter values or configurations
- ✅ References design tokens by name (e.g., "Base/White (#FCFDFE)")
- ✅ Describes interaction patterns (hover, focus, click)
- ✅ Notes any constraints or limitations
- ✅ Provides context for when to use this variant

#### 16.3.4 Component Reference Template

Use ASTooltip as the reference template when creating new stories. All new BlazingStories MUST match or exceed this level of detail.

**File structure:**
```
BlazingStories/Components/Stories/BaseComponents/
└── AS{ComponentName}/
    ├── AS{ComponentName}.stories.razor       (7+ stories minimum)
    └── AS{ComponentName}.stories.razor.css   (scoped styles)
```

**Minimum story count:** 7 stories (Interactive, 2+ variants, Comparison, Edge case, Real-world usage, Accessibility, Design tokens)

#### 16.3.5 PR Review Checklist for BlazingStories (MANDATORY)

When reviewing PRs that add or modify BlazingStories, Copilot MUST verify:

- [ ] Interactive story exists and uses `@attributes="context.Args"`
- [ ] At least 2 variant stories demonstrating different configurations
- [ ] Comparison story showing variants side-by-side (if component has variants)
- [ ] Edge case story addressing long content, empty states, or error handling
- [ ] Real-world usage story showing integration with other components
- [ ] ARIA Accessibility story with complete documentation
- [ ] Design Tokens Reference story with all token categories documented
- [ ] All stories have comprehensive `<Description>` tags
- [ ] Scoped `.stories.razor.css` file exists with semantic class names
- [ ] No inline styles in story templates
- [ ] Color tokens include hex values and Figma variable names
- [ ] Typography tokens include family, size, weight, line height
- [ ] Live examples included in design tokens story

**Verification command:** Compare new story against ASTooltip.stories.razor structure and completeness

#### 16.3.6 Exceptions and Flexibility

- **Simple components:** If a component is extremely simple (e.g., ASBadge with only color variants), some stories may be combined, but minimum requirements still apply
- **Complex components:** Components with extensive APIs may require additional stories beyond the minimum
- **Documentation-first:** When in doubt, over-document rather than under-document

#### 16.3.7 Benefits of This Standard

Following this comprehensive standard ensures:
- **Discoverability:** Developers can find and understand components quickly
- **Consistency:** All components documented to the same high standard
- **Accessibility:** ARIA patterns clearly documented and demonstrated
- **Design alignment:** Token documentation ensures Figma-to-code accuracy
- **Onboarding:** New developers can learn component APIs through examples
- **Quality:** Edge cases and real-world usage prevent integration issues

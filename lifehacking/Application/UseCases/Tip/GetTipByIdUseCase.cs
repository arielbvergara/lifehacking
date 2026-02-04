using Application.Dtos.Tip;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Primitives;
using Domain.ValueObject;

namespace Application.UseCases.Tip;

public class GetTipByIdUseCase(ITipRepository tipRepository, ICategoryRepository categoryRepository)
{
    public async Task<Result<TipDetailResponse, AppException>> ExecuteAsync(
        GetTipByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tipId = TipId.Create(request.Id);
        
        var tip = await tipRepository.GetByIdAsync(tipId, cancellationToken);
        if (tip is null)
        {
            return Result<TipDetailResponse, AppException>.Fail(
                new NotFoundException($"Tip with ID '{request.Id}' was not found."));
        }

        var category = await categoryRepository.GetByIdAsync(tip.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<TipDetailResponse, AppException>.Fail(
                new NotFoundException($"Category with ID '{tip.CategoryId.Value}' was not found."));
        }

        var response = tip.ToTipDetailResponse(category.Name);
        
        return Result<TipDetailResponse, AppException>.Ok(response);
    }
}
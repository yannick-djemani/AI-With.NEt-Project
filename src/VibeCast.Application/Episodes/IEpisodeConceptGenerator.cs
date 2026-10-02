using System;

namespace VibeCast.Application.Episodes;

public interface IEpisodeConceptGenerator
{
    Task<EpisodeConceptresult> GenerateEpisodeConceptAsync(
        GenerateEpisodeConceptRequest request,
        CancellationToken cancellationToken=default);

        IAsyncEnumerable<string> StreamAsync(
        GenerateEpisodeConceptRequest request,
         CancellationToken cancellationToken = default);

}

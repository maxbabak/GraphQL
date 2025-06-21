using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Caching.Memory;

namespace GraphQlPriject
{
    internal class Program
    {
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        static readonly string _baseUrl = "https://rickandmortyapi.com/graphql";
        private static readonly GraphQLHttpClient _client = new GraphQLHttpClient(
        $"{_baseUrl}",
        new NewtonsoftJsonSerializer());


        static async Task Main(string[] args)
        {
            while (true)
            {
                ShowMenu();
                Console.Write("Select an option: ");
                if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > 4)
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                    continue;
                }
                await HandleMenuSelectionAsync(selection);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }


        static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine("1. Get all heroes");
            Console.WriteLine("2. Get hero by ID");
            Console.WriteLine(("3. Get all episodes"));
            Console.WriteLine("4. Get sotred episodes by name");
        }

        static async Task HandleMenuSelectionAsync(int selection)
        {

            switch (selection)
            {
                case 1:
                    await GetAllHeroesAsync();
                    break;
                case 2:
                    Console.Write("Enter hero ID: ");
                    if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
                    {
                        Console.WriteLine("Invalid ID. Please enter a positive integer.");
                        return;
                    }
                    Console.Write("Enter properties to fetch (comma-separated, or press enter to leave the default blank): ");
                    string properties = Console.ReadLine()?.Trim() ?? string.Empty;
                    await GetHeroByIdAsync(id, properties);
                    break;
                case 3:
                    await GetAllEpisodesAsync();
                    break;
                case 4:
                    await GetSortedEpisodesByNameAsync();
                    break;
                default:
                    Console.WriteLine("Invalid selection. Please try again.");
                    break;
            }
        }


        static async Task GetAllHeroesAsync()
        {
            var request = new GraphQLRequest
            {
                Query = @"query {
                    characters {
                        results {
                            id
                            name
                            status
                            species
                            type
                            gender
                            origin {
                                id
                                name
                                type
                                dimension
                                residents {
                                    id
                                    name
                                }
                                created
                            }
                            location {
                                id
                                name
                                type
                                dimension
                                residents {
                                    id
                                    name
                                }
                                created
                            }
                            image
                            episode {
                                id
                                name
                                air_date
                                episode
                                characters {
                                    id
                                    name
                                }
                                created
                            }
                            created
                        }
                    }
                }"
            };

            var response = await _client.SendQueryAsync<dynamic>(request);
            Console.WriteLine(response.Data);
        }

        static async Task GetHeroByIdAsync(int id, string properties)
        {
            if (string.IsNullOrWhiteSpace(properties))
            {
                properties = @"id,name,status,species,type,gender,origin{id,name,type,dimension,residents{id,name},created},location{id,name,type,dimension,residents{id,name},created},image,episode{id,name,air_date,episode,characters{id,name},created},created";
            }

            string cacheKey = $"hero:{id}:{properties}";

            if (_cache.TryGetValue(cacheKey, out dynamic cachedHero))
            {
                Console.WriteLine("Fetching from cache...");
                Console.WriteLine(cachedHero);
                return;
            }

            var request = new GraphQLRequest
            {
                Query = @$"query ($id: ID!) {{
            character(id: $id) {{
                {properties}
            }}
        }}",
                Variables = new { id }
            };

            var response = await _client.SendQueryAsync<dynamic>(request);

            if (response.Errors != null && response.Errors.Any())
            {
                Console.WriteLine("Error fetching hero: " + string.Join(", ", response.Errors.Select(e => e.Message)));
                return;
            }

            if (response.Data == null)
            {
                Console.WriteLine("No data returned from the query.");
                return;
            }

            Console.WriteLine(response.Data);

            _cache.Set(cacheKey, (object)response.Data, TimeSpan.FromMinutes(15));
        }



        static async Task GetAllEpisodesAsync()
        {
            var request = new GraphQLRequest
            {
                Query = @"query {
                    episodes {
                        results {
                            id
                            name
                            air_date
                            episode
                            characters {
                                id
                                name
                            }
                            created
                        }
                    }
                }"
            };

            var response = await _client.SendQueryAsync<dynamic>(request);

            if (response.Errors != null && response.Errors.Any())
            {
                Console.WriteLine("Error fetching episodes: " + string.Join(", ", response.Errors.Select(e => e.Message)));
                return;
            }

            if (response.Data == null)
            {
                Console.WriteLine("No data returned from the query.");
                return;
            }

            Console.WriteLine(response.Data);
        }

        static async Task GetSortedEpisodesByNameAsync()
        {
            var request = new GraphQLRequest
            {
                Query = @"query {
                    episodes {
                        results {
                            id
                            name
                            air_date
                            episode
                            characters {
                                id
                                name
                            }
                            created
                        }
                    }
                }"
            };

            var response = await _client.SendQueryAsync<dynamic>(request);

            if (response.Errors != null && response.Errors.Any())
            {
                Console.WriteLine("Error fetching episodes: " + string.Join(", ", response.Errors.Select(e => e.Message)));
                return;
            }

            if (response.Data == null)
            {
                Console.WriteLine("No data returned from the query.");
                return;
            }

            var episodes = ((IEnumerable<dynamic>)response.Data.episodes.results)
                .OrderBy(e => (string)e.name)
                .ToList();

            foreach (var episode in episodes)
            {
                Console.WriteLine($"{episode.id}: {episode.name} - {episode.air_date}");
            }
        }

    }
}
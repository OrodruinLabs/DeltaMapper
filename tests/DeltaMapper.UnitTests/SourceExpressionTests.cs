using System.Linq.Expressions;
using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class SourceExpressionTests
{
    private class Source { public string FirstName { get; set; } = ""; }
    private class Dest { public string FullName { get; set; } = ""; }

    [Fact]
    public void MapFrom_preserves_raw_lambda_expression()
    {
        var options = new MemberOptions<Source>();
        Expression<Func<Source, string>> expr = s => s.FirstName;
        options.MapFrom(expr);

        options.SourceExpression.Should().NotBeNull();
        options.SourceExpression.Should().BeAssignableTo<Expression<Func<Source, string>>>();
    }

    [Fact]
    public void MapFrom_expression_is_stored_in_MemberConfiguration()
    {
        var profile = new TestProfile();
        var typeMap = profile.TypeMaps[0];
        var memberConfig = typeMap.MemberConfigurations[0];

        memberConfig.SourceExpression.Should().NotBeNull();
    }

    private class TestProfile : Profile
    {
        public TestProfile()
        {
            CreateMap<Source, Dest>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName));
        }
    }
}

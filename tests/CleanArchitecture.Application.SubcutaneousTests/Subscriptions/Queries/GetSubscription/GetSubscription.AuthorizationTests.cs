using CleanArchitecture.Application.Common.Security.Permissions;
using CleanArchitecture.Application.Common.Security.Roles;

namespace CleanArchitecture.Application.SubcutaneousTests.Subscriptions.Queries.GetSubscription;

public class GetSubscriptionAuthorizationTests
{
    private readonly IMediator _mediator;
    private readonly TestCurrentUserProvider _currentUserProvider;

    public GetSubscriptionAuthorizationTests()
    {
        var webAppFactory = new WebAppFactory();
        _mediator = webAppFactory.CreateMediator();
        _currentUserProvider = webAppFactory.TestCurrentUserProvider;
    }

    [Fact]
    public async Task GetSubscription_WhenDifferentUserButWithAdminRole_ShouldAuthorize()
    {
        // Arrange
        var currentUser = CurrentUserFactory.CreateCurrentUser(
            id: Constants.User.Id,
            roles: [Role.Admin]);

        _currentUserProvider.Returns(currentUser);

        var command = SubscriptionQueryFactory.CreateGetSubscriptionQuery(
            userId: Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.FirstError.Type.Should().NotBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task GetSubscription_WhenDifferentUserButWithoutAdminRole_ShouldAuthorize()
    {
        // Arrange
        var currentUser = CurrentUserFactory.CreateCurrentUser(
            id: Constants.User.Id,
            roles: []);

        _currentUserProvider.Returns(currentUser);

        var command = SubscriptionQueryFactory.CreateGetSubscriptionQuery(
            userId: Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task GetSubscription_WhenGettingForSelfButWithoutRequiredPermissions_ShouldNotAuthorize()
    {
        // Arrange
        var currentUser = CurrentUserFactory.CreateCurrentUser(
            id: Constants.User.Id,
            permissions: [],
            roles: []);

        _currentUserProvider.Returns(currentUser);

        var command = SubscriptionQueryFactory.CreateGetSubscriptionQuery(
            userId: Constants.User.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task GetSubscription_WhenGettingForSelfWithRequiredPermissions_ShouldAuthorize()
    {
        // Arrange
        var currentUser = CurrentUserFactory.CreateCurrentUser(
            id: Constants.User.Id,
            permissions: [Permission.Subscription.Get],
            roles: []);

        _currentUserProvider.Returns(currentUser);

        var command = SubscriptionQueryFactory.CreateGetSubscriptionQuery(
            userId: Constants.User.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.FirstError.Type.Should().NotBe(ErrorType.Unauthorized);
    }
}
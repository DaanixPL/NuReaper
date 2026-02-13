using App.Application.Commands.Users.UpdateUser;
using App.Application.Validators.Exceptions;
using App.Domain.Abstractions;
using App.Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace App.Application.Tests.Commands.Users.UpdateUser
{
    public class UpdateUserCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        [Fact]
        public async Task Handle_UserExists_UpdateUserAndReturnsId()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test", PasswordHash = "old", Email = "test@test.com" };
            var command = new UpdateUserCommand { UserId = 1, PasswordHash = "newpass" };

            _userRepositoryMock
                .Setup(repo => repo.GetUserByIdAsync(1, default)).ReturnsAsync(user);

            _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);

            var handler = new UpdateUserCommandHandler( _userRepositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            _mapperMock.Verify(m => m.Map(command, user), Times.Once);
            _userRepositoryMock.Verify(r => r.UpdateUserAsync(user, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = new UpdateUserCommand { UserId = 2, PasswordHash = "newpass" };
            _userRepositoryMock.Setup(r => r.GetUserByIdAsync(2, default)).ReturnsAsync((User?)null);
            _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);

            var handler = new UpdateUserCommandHandler(_userRepositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, default));
        }
    }
}

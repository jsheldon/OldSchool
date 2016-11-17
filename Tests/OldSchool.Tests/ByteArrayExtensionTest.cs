using OldSchool.Ifx;
using Xunit;

namespace OldSchool.Tests
{
    public class ByteArrayExtensionTest
    {
        [Fact]
        public void Calling_Trim_Will_Return_A_Trimmed_Byte_Array()
        {
            var main = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { 3, 4, 5, 6, 7, 8, 9 };
            var returnValue = main.Trim(2);

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void Calling_Trim_With_A_Value_Equal_To_The_Length_Will_Return_An_Empty_Byte_Array()
        {
            var main = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { };
            var returnValue = main.Trim(9);

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void Calling_Trim_With_A_Value_Greater_Than_The_Length_Return_An_Empty_Byte_Array()
        {
            var main = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { };
            var returnValue = main.Trim(10);

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void When_Calling_Append_The_Return_Value_Will_Be_The_Sum_Of_Two_Arrays()
        {
            // Arrange
            var part1 = new byte[] { 1, 2, 3, 4, 5 };
            var part2 = new byte[] { 6, 7, 8, 9, 10 };
            var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            // Act
            var returnValue = part1.Append(part2);

            // Assert
            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void When_Calling_Locate_All_Matching_Indexes_Will_Be_Found()
        {
            var main = new byte[] { 1, 7, 2, 5, 1, 9, 10, 2 };

            var returnValue = main.Locate(2);

            Assert.Equal(2, returnValue.Length);
            Assert.Equal(2, returnValue[0]);
            Assert.Equal(7, returnValue[1]);
        }

        [Fact]
        public void When_Calling_Locate_First_The_Response_Will_Be_Correct_If_The_Value_Is_Found()
        {
            var main = new byte[] { 1, 7, 2, 5, 1, 9, 10, 2 };

            var returnValue = main.LocateFirst(2);

            Assert.Equal(2, returnValue);
        }

        [Fact]
        public void When_Calling_Locate_First_The_Response_Will_Be_Null_If_The_Value_Is_Not_Found()
        {
            var main = new byte[] { 1, 2, 3, 4, 6, 7, 8, 9 };

            var returnValue = main.LocateFirst(5);

            Assert.Null(returnValue);
        }

        [Fact]
        public void When_Calling_Locate_The_Return_Value_Will_Be_Empty_If_No_Values_Found()
        {
            var main = new byte[] { 1, 7, 2, 5, 1, 9, 10, 2 };

            var returnValue = main.Locate(6);

            Assert.Equal(0, returnValue.Length);
        }

        [Fact]
        public void When_Calling_Substring_The_Return_Value_Will_Contain_Only_The_Bytes_Requested()
        {
            // Arrange
            var main = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { 4, 5, 6 };

            // Act
            var returnValue = main.Substring(3, 3);

            // Assert
            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void When_Calling_Substring_With_Only_The_Length_The_Return_Value_Will_Contain_Only_The_First_Bytes_Requested()
        {
            // Arrange
            var main = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var expected = new byte[] { 4, 5, 6, 7, 8, 9 };

            // Act
            var returnValue = main.Substring(3);

            // Assert
            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void When_Calling_RemoveBackspaces_With_A_Single_Backspace_The_Backspaace_Is_Removed_And_The_Remaining_Bytes_Are_Shifted_Left()
        {
            var main = new byte[] { 1, 1, 1, 1, 1, 1, 1, 8, 1, 1, 1, 1, };
            var expected = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, };

            var returnValue = main.RemoveBackspaces();

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void When_Calling_RemoveBackspaces_With_A_Multiple_Backspaces_The_Backspaaces_Are_All_Removed_And_The_Remaining_Bytes_Are_Shifted_Left()
        {
            var main = new byte[] { 1, 8, 2, 2, 8, 3, 3, 3, 8, 4, 4, 4, 4 };
            var expected = new byte[] { 2, 3, 3, 4, 4, 4, 4 };

            var returnValue = main.RemoveBackspaces();

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void When_Calling_RemoveBackspaces_When_The_First_Character_Is_A_Backspace_Will_Throw_No_Errors()
        {
            var main = new byte[] { 8, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            var expected = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 };

            var returnValue = main.RemoveBackspaces();

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void When_Calling_RemoveBackspaces_When_The_Last_Character_Is_A_Backspace_Will_Throw_No_Errors()
        {
            var main = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 8 };
            var expected = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 };

            var returnValue = main.RemoveBackspaces();

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void ExpandBackspacesTest1()
        {
            var main = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 8 };
            var expected = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 8, 32, 8 };

            var returnValue = main.ExpandBackspaces();

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void ExpandBackspacesTest2()
        {
            var main = new byte[] { 8, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            var expected = new byte[] { 8, 32, 8, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

            var returnValue = main.ExpandBackspaces();

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void ExpandBackspacesTest3()
        {
            var main = new byte[] { 1, 8, 2, 2, 8, 3, 3, 3, 8, 4, 4, 4, 4 };
            var expected = new byte[] { 1, 8, 32, 8, 2, 2, 8, 32, 8, 3, 3, 3, 8, 32, 8, 4, 4, 4, 4 };

            var returnValue = main.ExpandBackspaces();

            Assert.Equal(expected, returnValue);
        }

        [Fact]
        public void ExpandBackspacesTest4()
        {
            var main = new byte[] { 1, 1, 1, 1, 1, 1, 1, 8, 1, 1, 1, 1, };
            var expected = new byte[] { 1, 1, 1, 1, 1, 1, 1, 8, 32, 8, 1, 1, 1, 1, };

            var returnValue = main.ExpandBackspaces();

            Assert.Equal(expected, returnValue);
        }
    }
}
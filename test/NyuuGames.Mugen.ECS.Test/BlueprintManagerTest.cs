namespace NyuuGames.Mugen.ECS.Test
{
    using System;
    using System.Runtime.CompilerServices;
    using FluentAssertions;
    using FluentAssertions.Common;
    using Xunit;

    public class BlueprintManagerTest
    {
        [Fact]
        public unsafe void CreateABlueprint()
        {
            var blueprintManager = new BlueprintManager();

            Span<ComponentType> componentTypes = stackalloc ComponentType[3];

            componentTypes[0] = new ComponentType(0, 16);
            componentTypes[1] = new ComponentType(1, 16);
            componentTypes[2] = new ComponentType(2, 16);

            var blueprint = blueprintManager.GetBlueprint(componentTypes);

            ref var definition = ref Unsafe.AsRef<BlueprintDefinition>(blueprint._definition);

            definition.ComponentTypesLength.Should().Be(componentTypes.Length);
            definition.ComponentTypes.SequenceEqual(componentTypes).Should().BeTrue();
        }

        [Fact]
        public unsafe void CreateBlueprintWithoutComponentTypes()
        {
            var blueprintManager = new BlueprintManager();

            Span<ComponentType> componentTypes = stackalloc ComponentType[0];

            var blueprint = blueprintManager.GetBlueprint(componentTypes);

            ref var definition = ref Unsafe.AsRef<BlueprintDefinition>(blueprint._definition);

            definition.ComponentTypesLength.Should().Be(componentTypes.Length);
            definition.ComponentTypes.SequenceEqual(componentTypes).Should().BeTrue();
        }

        [Fact]
        public unsafe void CreateABlueprintAndRetrieveTheSame()
        {
            var blueprintManager = new BlueprintManager();

            Span<ComponentType> componentTypes = stackalloc ComponentType[3];

            componentTypes[0] = new ComponentType(0, 16);
            componentTypes[1] = new ComponentType(1, 16);
            componentTypes[2] = new ComponentType(2, 16);

            var blueprint = blueprintManager.GetBlueprint(componentTypes);

            var blueprint2 = blueprintManager.GetBlueprint(componentTypes);

            (blueprint2._definition == blueprint._definition).Should().BeTrue();
        }

        [Fact]
        public unsafe void CreateTwoBlueprintsDifferentAmountOfComponentTypes()
        {
            var blueprintManager = new BlueprintManager();

            Span<ComponentType> componentTypes = stackalloc ComponentType[3];

            componentTypes[0] = new ComponentType(0, 16);
            componentTypes[1] = new ComponentType(1, 16);
            componentTypes[2] = new ComponentType(2, 16);

            var blueprint = blueprintManager.GetBlueprint(componentTypes);

            Span<ComponentType> componentTypes2 = stackalloc ComponentType[4];

            componentTypes2[0] = new ComponentType(0, 16);
            componentTypes2[1] = new ComponentType(1, 16);
            componentTypes2[2] = new ComponentType(2, 16);
            componentTypes2[3] = new ComponentType(3, 16);

            var blueprint2 = blueprintManager.GetBlueprint(componentTypes2);

            (blueprint2._definition == blueprint._definition).Should().BeFalse();
        }

        [Fact]
        public unsafe void CreateTwoBlueprintsCompleteDifferentComponentTypes()
        {
            var blueprintManager = new BlueprintManager();

            Span<ComponentType> componentTypes = stackalloc ComponentType[3];

            componentTypes[0] = new ComponentType(0, 16);
            componentTypes[1] = new ComponentType(1, 16);
            componentTypes[2] = new ComponentType(2, 16);

            var blueprint = blueprintManager.GetBlueprint(componentTypes);

            Span<ComponentType> componentTypes2 = stackalloc ComponentType[3];

            componentTypes2[0] = new ComponentType(3, 16);
            componentTypes2[1] = new ComponentType(4, 16);
            componentTypes2[2] = new ComponentType(5, 16);

            var blueprint2 = blueprintManager.GetBlueprint(componentTypes2);

            (blueprint2._definition == blueprint._definition).Should().BeFalse();
        }

        [Fact]
        public unsafe void CreateTwoBlueprintsWithDifferentComponentTypes()
        {
            var blueprintManager = new BlueprintManager();

            Span<ComponentType> componentTypes = stackalloc ComponentType[3];

            componentTypes[0] = new ComponentType(0, 16);
            componentTypes[1] = new ComponentType(1, 16);
            componentTypes[2] = new ComponentType(2, 16);

            var blueprint = blueprintManager.GetBlueprint(componentTypes);

            Span<ComponentType> componentTypes2 = stackalloc ComponentType[3];

            componentTypes2[0] = new ComponentType(0, 16);
            componentTypes2[1] = new ComponentType(1, 16);
            componentTypes2[2] = new ComponentType(5, 16);

            var blueprint2 = blueprintManager.GetBlueprint(componentTypes2);

            (blueprint2._definition == blueprint._definition).Should().BeFalse();
        }
    }
}
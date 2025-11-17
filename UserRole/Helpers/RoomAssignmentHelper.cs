namespace UserRoles.Helpers
{
    public static class RoomAssignmentHelper
    {
        // Room assignments per grade level and section
        private static readonly Dictionary<string, Dictionary<int, string>> RoomAssignments = new()
        {
            // Grade 1 - Ground Floor, Building A
            ["1"] = new Dictionary<int, string>
            {
                [1] = "Room 101 - Building A",
                [2] = "Room 102 - Building A",
                [3] = "Room 103 - Building A",
                [4] = "Room 104 - Building A",
                [5] = "Room 105 - Building A",
                [6] = "Room 106 - Building A",
                [7] = "Room 107 - Building A",
                [8] = "Room 108 - Building A"
            },

            // Grade 2 - Ground Floor, Building B
            ["2"] = new Dictionary<int, string>
            {
                [1] = "Room 201 - Building B",
                [2] = "Room 202 - Building B",
                [3] = "Room 203 - Building B",
                [4] = "Room 204 - Building B",
                [5] = "Room 205 - Building B",
                [6] = "Room 206 - Building B",
                [7] = "Room 207 - Building B",
                [8] = "Room 208 - Building B"
            },

            // Grade 3 - Second Floor, Building A
            ["3"] = new Dictionary<int, string>
            {
                [1] = "Room 301 - Building A",
                [2] = "Room 302 - Building A",
                [3] = "Room 303 - Building A",
                [4] = "Room 304 - Building A",
                [5] = "Room 305 - Building A",
                [6] = "Room 306 - Building A",
                [7] = "Room 307 - Building A",
                [8] = "Room 308 - Building A"
            },

            // Grade 4 - Second Floor, Building B
            ["4"] = new Dictionary<int, string>
            {
                [1] = "Room 401 - Building B",
                [2] = "Room 402 - Building B",
                [3] = "Room 403 - Building B",
                [4] = "Room 404 - Building B",
                [5] = "Room 405 - Building B",
                [6] = "Room 406 - Building B",
                [7] = "Room 407 - Building B",
                [8] = "Room 408 - Building B"
            },

            // Grade 5 - Third Floor, Building A
            ["5"] = new Dictionary<int, string>
            {
                [1] = "Room 501 - Building A",
                [2] = "Room 502 - Building A",
                [3] = "Room 503 - Building A",
                [4] = "Room 504 - Building A",
                [5] = "Room 505 - Building A",
                [6] = "Room 506 - Building A",
                [7] = "Room 507 - Building A",
                [8] = "Room 508 - Building A"
            },

            // Grade 6 - Third Floor, Building B
            ["6"] = new Dictionary<int, string>
            {
                [1] = "Room 601 - Building B",
                [2] = "Room 602 - Building B",
                [3] = "Room 603 - Building B",
                [4] = "Room 604 - Building B",
                [5] = "Room 605 - Building B",
                [6] = "Room 606 - Building B",
                [7] = "Room 607 - Building B",
                [8] = "Room 608 - Building B"
            }
        };

        public static string GetRoomForSection(string gradeLevel, int section)
        {
            if (RoomAssignments.TryGetValue(gradeLevel, out var sections))
            {
                if (sections.TryGetValue(section, out var room))
                {
                    return room;
                }
            }
            return $"Room {gradeLevel}{section:D2}";
        }

        public static Dictionary<int, string> GetAllRoomsForGrade(string gradeLevel)
        {
            if (RoomAssignments.TryGetValue(gradeLevel, out var rooms))
            {
                return rooms;
            }
            return new Dictionary<int, string>();
        }

        public static string GetBuildingForGrade(string gradeLevel)
        {
            return gradeLevel switch
            {
                "1" => "Building A - Ground Floor",
                "2" => "Building B - Ground Floor",
                "3" => "Building A - Second Floor",
                "4" => "Building B - Second Floor",
                "5" => "Building A - Third Floor",
                "6" => "Building B - Third Floor",
                _ => "TBD"
            };
        }
    }
}
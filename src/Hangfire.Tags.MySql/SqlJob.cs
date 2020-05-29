// https://github.com/arnoldasgudas/Hangfire.MySqlStorage/blob/master/Hangfire.MySql/Entities/SqlJob.cs
// This file is part of Hangfire.MySqlStorage.
// Copyright � 2018 Arnold Asgudas <https://github.com/arnoldasgudas/Hangfire.MySqlStorage>.
//
// Hangfire.MySqlStorage is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, either version 3
// of the License, or any later version.
//
// Hangfire.MySqlStorage  is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with Hangfire.PostgreSql. If not, see <http://www.gnu.org/licenses/>.
//
// This work is based on the work of Sergey Odinokov, author of
// Hangfire. <http://hangfire.io/>
//
//    Special thanks goes to him.

using System;

namespace Hangfire.Tags.MySql
{
    internal class SqlJob
    {
        public int Id { get; set; }
        public string InvocationData { get; set; }
        public string Arguments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpireAt { get; set; }

        public DateTime? FetchedAt { get; set; }

        public string StateName { get; set; }
        public string StateReason { get; set; }
        public string StateData { get; set; }
    }
}

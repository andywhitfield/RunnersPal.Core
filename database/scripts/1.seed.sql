insert into UserAccount(DisplayName, CreatedDate, LastActivityDate, EmailAddress, OriginalHostAddress, UserType, DistanceUnits)
select 'Admin', datetime('now'), datetime('now'), 'admin@nosuchblogger.com', 'runnerspal.nosuchblogger.com', 'A', 0
where not exists(select 1 from UserAccount where DisplayName = 'Admin' and UserType = 'A');

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select '1 Kilometer', 1, 1, u.Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = '1 Kilometer' and r.Creator = u.Id);

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select '1 Mile', 1, 0, Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = '1 Mile' and r.Creator = u.Id);

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select '3 Miles', 3, 0, Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = '3 Miles' and r.Creator = u.Id);

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select '5 Kilometers', 5, 1, Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = '5 Kilometers' and r.Creator = u.Id);

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select '10 Kilometers', 10, 1, Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = '10 Kilometers' and r.Creator = u.Id);

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select '10 Miles', 10, 0, Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = '10 Miles' and r.Creator = u.Id);

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select 'Half-marathon', 13.109375, 0, Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = 'Half-marathon' and r.Creator = u.Id);

insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select 'Marathon', 26.21875, 0, Id, datetime('now'), 'Z' from UserAccount u
where u.DisplayName = 'Admin' and u.UserType = 'A' and not exists(select 1 from Route r where r.Name = 'Marathon' and r.Creator = u.Id);

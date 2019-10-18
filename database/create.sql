CREATE TABLE UserAccount(
	Id integer PRIMARY KEY AUTOINCREMENT NOT NULL,
	DisplayName nvarchar(200) NOT NULL,
	CreatedDate datetime NOT NULL,
	LastActivityDate datetime NOT NULL,
	EmailAddress nvarchar(200) NULL,
	OriginalHostAddress nvarchar(50) NOT NULL,
	UserType char(1) NOT NULL,
	DistanceUnits int NOT NULL
);

CREATE TABLE Route(
	Id integer PRIMARY KEY AUTOINCREMENT NOT NULL,
	Name nvarchar NOT NULL,
	Notes text NULL,
	Distance numeric NOT NULL,
	DistanceUnits int NOT NULL,
	Creator bigint NOT NULL,
	CreatedDate datetime NOT NULL,
	RouteType char(1) NOT NULL,
	MapPoints text NULL,
	ReplacesRouteId bigint NULL,
    FOREIGN KEY(ReplacesRouteId) REFERENCES Route(Id),
    FOREIGN KEY(Creator) REFERENCES UserAccount(Id)
);

CREATE TABLE RunLog(
	Id integer PRIMARY KEY AUTOINCREMENT NOT NULL,
	Date datetime NOT NULL,
	RouteId bigint NOT NULL,
	TimeTaken varchar(50) NOT NULL,
	UserAccountId bigint NOT NULL,
	CreatedDate datetime NOT NULL,
	Comment nvarchar(2000) NULL,
	LogState char(1) NOT NULL,
	ReplacesRunLogId bigint NULL,
    FOREIGN KEY(RouteId) REFERENCES Route(Id),
    FOREIGN KEY(ReplacesRunLogId) REFERENCES RunLog(Id),
    FOREIGN KEY(UserAccountId) REFERENCES UserAccount(Id)
);

CREATE TABLE SiteSettings(
	Id integer PRIMARY KEY AUTOINCREMENT NOT NULL,
	Domain nvarchar(50) NOT NULL,
	Identifier nvarchar(50) NOT NULL,
	SettingValue text NULL
);

CREATE TABLE UserAccountAuthentication(
	Id integer PRIMARY KEY AUTOINCREMENT NOT NULL,
	UserAccountId bigint NOT NULL,
	Identifier nvarchar(500) NOT NULL,
    FOREIGN KEY(UserAccountId) REFERENCES UserAccount(Id)
);

CREATE TABLE UserPref(
	Id integer PRIMARY KEY AUTOINCREMENT NOT NULL,
	UserAccountId bigint NOT NULL,
	ValidTo datetime NULL,
	Weight float NULL,
	WeightUnits nvarchar(5) NULL,
    FOREIGN KEY(UserAccountId) REFERENCES UserAccount(Id)
);

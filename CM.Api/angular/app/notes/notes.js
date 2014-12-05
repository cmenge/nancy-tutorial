angular.module('cm.notes', [], ['$stateProvider', function ($stateProvider) {
    $stateProvider.
        state('notes', { url: '/notes', template: '<div ui-view></div>', controller: NotesBaseController }).
        state('notes.list', { url: '^/', templateUrl: '/angular/app/notes/list.html', controller: NotesListController }).
        state('notes.create', { url: '/create', templateUrl: '/angular/app/notes/create.html', controller: NotesCreateController }).
        state('notes.edit', { url: '/:noteId/edit', templateUrl: '/angular/app/notes/edit.html', controller: NotesEditController })
}]).factory('Note', ['$resource', function ($resource) {
    return $resource(apiBaseUrl + 'notes/:noteId', { noteId: '@Id' }, {
        query: { method: 'GET', params: { skip: 0, take: 100 }, isArray: true },
        update: { method: 'PUT', params: {} }
    });
}]);

var NotesBaseController = function ($scope) { }
NotesBaseController.$inject = ['$scope'];

var NotesEditController = function ($scope, Note, $stateParams) {
    $scope.model = Note.get({ noteId: $stateParams.noteId });
    
    $scope.save = function () {
        $scope.model.$update({}, function () {
            $scope.showMessage({ message: "Saved!", type: "success" });
        });
    };
}
NotesEditController.$inject = ['$scope', 'Note', '$stateParams'];

var NotesCreateController = function ($scope, $http, $state, Note) {
    $scope.model = new Note();
    
    $scope.save = function () {
        $scope.model.$save({}, function (data) {
            $scope.showMessage({ message: "Note Created!", type: "success" });
            $state.transitionTo("notes.list");
        });
    };
};

NotesCreateController.$inject = ['$scope', '$http', '$state', 'Note'];

var NotesListController = function ($scope, Note) {

    $scope.data = Note.query({}, function () {
        console.log("done. resolved: ", $scope.data.$resolved);
    }, function (error) {
        $scope.error = error.data;
    });
    console.log("resolved: ", $scope.data.$resolved);
    $scope.deleteNote = function (index) {
        $scope.data[index].$delete({}, function () {
            $scope.data.splice(index, 1);
        });
    }
};
NotesListController.$inject = ['$scope', 'Note'];

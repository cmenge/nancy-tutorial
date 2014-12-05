'use strict';

var baseUrl = "/";
var apiBaseUrl = baseUrl + "api/v1/";

var cm = angular.module('cm', ['ui.router', 'ui.bootstrap', 'ngResource', 'cm.notes' ]);

cm.config(['$httpProvider', '$modalProvider', '$urlRouterProvider', function ($httpProvider, $modalProvider, $urlRouterProvider) {
      $urlRouterProvider
        // If the url is ever invalid, e.g. '/asdf', then redirect to '/' aka the home state
        .otherwise('/');

      var interceptor = ['$rootScope', '$q', function (scope, $q) {
          function success(response) {
              return response;
          }

          function errorIntercept(response) {
              var status = response.status;
              var message = response.data.Message || response.data.error_description;
              scope.showErrorBox(message);
              return $q.reject(response);
          }

          return function (promise) {
              return promise.then(success, errorIntercept);
          };
      }];

      $httpProvider.responseInterceptors.push(interceptor);
  }]).run(['$rootScope', '$location', '$http', '$modal', '$state', '$stateParams', function ($rootScope, $location, $http, $modal, $state, $stateParams) {
      $rootScope.$state = $state;
      $rootScope.$stateParams = $stateParams;

      $rootScope.showMessage = function (message) {
          if (!$rootScope.messages)
              $rootScope.messages = [];
          $rootScope.messages.push(message);
      }

      $rootScope.removeMessage = function (index) {
          $rootScope.messages.splice(index, 1);
      }

      $rootScope.showErrorBox = function (message) {
          var modalInstance = $modal.open({
              templateUrl: "angular/modals/error.html?1",
              controller: ErrorController,
              resolve: {
                  err: function () {
                      return { message: message };
                  }
              }
          });
      };
  }]);

var ErrorController = function ($scope, $modalInstance, err) {
    $scope.error = err;
    $scope.close = function () {
        $modalInstance.close();
    };
};
ErrorController.$inject = ["$scope", "$modalInstance", "err"];
